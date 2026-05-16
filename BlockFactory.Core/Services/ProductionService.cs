using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.DTOs.Production;
using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Inventory;
using BlockFactory.Core.Models.Production;
using BlockFactory.Core.Session;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Core.Services
{
    public class ProductionService : IProductionService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAuthService _authService;

        public ProductionService(IUnitOfWork uow, IAuthService authService)
        {
            _uow = uow;
            _authService = authService;
        }

        // ─── ملخص يومي ──────────────────────────────
        public async Task<DailyProductionSummaryDto> GetDailySummaryAsync(
            DateTime date)
        {
            var records = await _uow.Productions.Query()
                .Include(p => p.Product)
                    .ThenInclude(p => p.ProductType)
                .Include(p => p.MaterialUsages)
                    .ThenInclude(m => m.RawMaterial)
                .Where(p => p.ProductionDate.Date == date.Date)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            var dto = new DailyProductionSummaryDto
            {
                Date = date,
                TotalProduced = records.Sum(r => r.QuantityProduced),
                TotalDefective = records.Sum(r => r.QuantityDefective),
                TotalNet = records.Sum(r => r.QuantityNet),
                Records = records.Select(MapToListDto).ToList()
            };

            // حساب المواد المستهلكة
            var materialGroups = records
                .SelectMany(r => r.MaterialUsages)
                .GroupBy(m => m.RawMaterial?.Name ?? "-");

            foreach (var group in materialGroups)
            {
                var material = group.First().RawMaterial;
                dto.MaterialsConsumed.Add(new MaterialConsumptionDto
                {
                    MaterialName = group.Key,
                    TotalConsumed = group.Sum(m => m.QuantityUsed),
                    Unit = GetUnitAr(material?.Unit),
                    RemainingStock = material?.QuantityAvailable ?? 0,
                    IsLow = material != null &&
                            material.QuantityAvailable <=
                            material.MinimumThreshold
                });
            }

            return dto;
        }

        // ─── سجلات بتاريخ ───────────────────────────
        public async Task<IEnumerable<ProductionRecordListDto>>
            GetByDateRangeAsync(DateTime from, DateTime to)
        {
            var records = await _uow.Productions.Query()
                .Include(p => p.Product)
                    .ThenInclude(p => p.ProductType)
                .Include(p => p.MaterialUsages)
                    .ThenInclude(m => m.RawMaterial)
                .Where(p =>
                    p.ProductionDate.Date >= from.Date &&
                    p.ProductionDate.Date <= to.Date)
                .OrderByDescending(p => p.ProductionDate)
                .ToListAsync();

            return records.Select(MapToListDto);
        }

        // ─── إحصائيات الإنتاج ───────────────────────
        public async Task<ProductionStatsDto> GetStatsAsync()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var todayRecords = await _uow.Productions.Query()
                .Where(p => p.ProductionDate.Date == today)
                .ToListAsync();

            var weekTotal = await _uow.Productions.Query()
                .Where(p => p.ProductionDate.Date >= weekStart)
                .SumAsync(p => p.QuantityNet);

            var monthTotal = await _uow.Productions.Query()
                .Where(p => p.ProductionDate.Date >= monthStart)
                .SumAsync(p => p.QuantityNet);

            var todayTotal = todayRecords.Sum(r => r.QuantityProduced);
            var todayDefective = todayRecords.Sum(r => r.QuantityDefective);

            // إحصائيات بالنوع
            var products = await _uow.Products.Query()
                .Include(p => p.Stock)
                .Where(p => p.IsActive)
                .ToListAsync();

            var byType = new List<ProductionByTypeDto>();
            foreach (var product in products)
            {
                var todayQty = todayRecords
                    .Where(r => r.ProductId == product.Id)
                    .Sum(r => r.QuantityNet);

                var monthQty = await _uow.Productions.Query()
                    .Where(p =>
                        p.ProductId == product.Id &&
                        p.ProductionDate.Date >= monthStart)
                    .SumAsync(p => p.QuantityNet);

                if (todayQty > 0 || monthQty > 0)
                {
                    byType.Add(new ProductionByTypeDto
                    {
                        ProductName = product.Name,
                        TodayQty = todayQty,
                        MonthQty = monthQty,
                        StockQty = product.Stock?.QuantityAvailable ?? 0
                    });
                }
            }

            return new ProductionStatsDto
            {
                TodayTotal = todayTotal,
                WeekTotal = weekTotal,
                MonthTotal = monthTotal,
                TodayDefective = todayDefective,
                DefectiveRate = todayTotal > 0
                    ? Math.Round((double)todayDefective / todayTotal * 100, 1)
                    : 0,
                ByProductType = byType
            };
        }

        // ─── تسجيل إنتاج ────────────────────────────
        public async Task<ServiceResult<int>> CreateProductionRecordAsync(
            CreateProductionDto dto)
        {
            // التحقق من البيانات
            if (dto.ProductId <= 0)
                return ServiceResult<int>.Fail("يجب اختيار نوع البلوك");

            if (dto.QuantityProduced <= 0)
                return ServiceResult<int>.Fail(
                    "الكمية المنتجة يجب أن تكون أكبر من صفر");

            if (dto.QuantityDefective > dto.QuantityProduced)
                return ServiceResult<int>.Fail(
                    "الكمية التالفة لا يمكن أن تتجاوز الكمية المنتجة");

            // التحقق من كفاية المواد الخام
            foreach (var usage in dto.MaterialUsages)
            {
                var material = await _uow.RawMaterials
                    .GetByIdAsync(usage.RawMaterialId);

                if (material == null) continue;

                if (material.QuantityAvailable < usage.QuantityUsed)
                    return ServiceResult<int>.Fail(
                        $"مخزون {material.Name} غير كافٍ. " +
                        $"المتاح: {material.QuantityAvailable} " +
                        $"{GetUnitAr(material.Unit)}");
            }

            await _uow.BeginTransactionAsync();
            try
            {
                var quantityNet = dto.QuantityProduced - dto.QuantityDefective;

                // إنشاء سجل الإنتاج
                var record = new ProductionRecord
                {
                    ProductionDate = dto.ProductionDate,
                    ProductId = dto.ProductId,
                    Shift = dto.Shift,
                    QuantityProduced = dto.QuantityProduced,
                    QuantityDefective = dto.QuantityDefective,
                    QuantityNet = quantityNet,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = CurrentSession.Instance.UserId
                };

                await _uow.Productions.AddAsync(record);
                await _uow.SaveChangesAsync();

                // تسجيل استهلاك المواد الخام
                foreach (var usage in dto.MaterialUsages)
                {
                    if (usage.QuantityUsed <= 0) continue;

                    var materialUsage = new ProductionMaterialUsage
                    {
                        ProductionRecordId = record.Id,
                        RawMaterialId = usage.RawMaterialId,
                        QuantityUsed = usage.QuantityUsed,
                        Unit = usage.Unit,
                        CreatedAt = DateTime.Now
                    };
                    await _uow.ProductionMaterialUsages.AddAsync(
                        materialUsage);

                    var material = await _uow.RawMaterials
                        .GetByIdAsync(usage.RawMaterialId);

                    if (material != null)
                    {
                        var before = material.QuantityAvailable;
                        material.QuantityAvailable -= usage.QuantityUsed;
                        material.UpdatedAt = DateTime.Now;
                        _uow.RawMaterials.Update(material);

                        var rawTrans = new RawMaterialTransaction
                        {
                            RawMaterialId = usage.RawMaterialId,
                            Type = RawMaterialTransactionType.ProductionOut,
                            Quantity = usage.QuantityUsed,
                            QuantityBefore = before,
                            QuantityAfter = material.QuantityAvailable,
                            UnitCost = material.CurrentUnitCost,
                            TotalCost = usage.QuantityUsed *
                                        material.CurrentUnitCost,
                            TransactionDate = DateTime.Now,
                            Reference = $"PROD-{record.Id}",
                            CreatedAt = DateTime.Now
                        };
                        await _uow.RawMaterialTransactions.AddAsync(
                            rawTrans);
                    }
                }

                // إضافة للمخزون
                await AddToInventoryAsync(
                    dto.ProductId, quantityNet, $"PROD-{record.Id}");

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                await _authService.LogActivityAsync(
                    "CreateProduction", "Production", record.Id,
                    newValues:
                        $"{dto.ProductName}: {dto.QuantityProduced} قطعة");

                return ServiceResult<int>.Ok(record.Id,
                    $"تم تسجيل إنتاج {quantityNet} قطعة بنجاح");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult<int>.Fail(
                    $"خطأ في تسجيل الإنتاج: {ex.Message}");
            }
        }

        // ─── حذف سجل إنتاج ──────────────────────────
        public async Task<ServiceResult> DeleteProductionRecordAsync(
            int recordId)
        {
            var record = await _uow.Productions.Query()
                .Include(p => p.MaterialUsages)
                .FirstOrDefaultAsync(p => p.Id == recordId);

            if (record == null)
                return ServiceResult.Fail("السجل غير موجود");

            // التحقق أن اليوم هو نفس يوم الإنتاج
            if (record.ProductionDate.Date != DateTime.Today)
                return ServiceResult.Fail(
                    "لا يمكن حذف سجلات إنتاج من أيام سابقة");

            await _uow.BeginTransactionAsync();
            try
            {
                // إرجاع المواد الخام
                foreach (var usage in record.MaterialUsages)
                {
                    var material = await _uow.RawMaterials
                        .GetByIdAsync(usage.RawMaterialId);
                    if (material != null)
                    {
                        material.QuantityAvailable += usage.QuantityUsed;
                        material.UpdatedAt = DateTime.Now;
                        _uow.RawMaterials.Update(material);
                    }
                }

                // خصم من المخزون
                await AddToInventoryAsync(
                    record.ProductId, -record.QuantityNet,
                    $"PROD-DEL-{record.Id}");

                // حذف السجل
                record.IsDeleted = true;
                record.UpdatedAt = DateTime.Now;
                _uow.Productions.Update(record);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                return ServiceResult.Ok("تم حذف سجل الإنتاج");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult.Fail($"خطأ: {ex.Message}");
            }
        }

        // ─── وصفة الإنتاج ───────────────────────────
        public async Task<IEnumerable<CreateMaterialUsageDto>>
            GetFormulaAsync(int productId)
        {
            var formulas = await _uow.ProductionFormulas.Query()
                .Include(f => f.RawMaterial)
                .Where(f => f.ProductId == productId && !f.IsDeleted)
                .OrderBy(f => f.RawMaterialId)
                .ToListAsync();

            if (formulas.Count > 0)
            {
                return formulas.Select(f => new CreateMaterialUsageDto
                {
                    RawMaterialId = f.RawMaterialId,
                    MaterialName = f.RawMaterial?.Name ?? "-",
                    QuantityUsed = 0,
                    Unit = string.IsNullOrWhiteSpace(f.Unit)
                        ? GetUnitAr(f.RawMaterial?.Unit)
                        : f.Unit
                });
            }

            var materials = await _uow.RawMaterials.Query()
                .Where(m => m.IsActive && !m.IsDeleted)
                .OrderBy(m => m.Name)
                .ToListAsync();

            return materials.Select(m => new CreateMaterialUsageDto
            {
                RawMaterialId = m.Id,
                MaterialName = m.Name,
                QuantityUsed = 0,
                Unit = GetUnitAr(m.Unit)
            });
        }

        // ─── Helpers ────────────────────────────────
        private async Task AddToInventoryAsync(
            int productId, int quantity, string? reference)
        {
            if (quantity == 0)
                return;

            if (quantity > 0)
            {
                await _uow.Inventory.UpdateStockAsync(
                    productId, quantity, TransactionType.ProductionIn,
                    reference);
            }
            else
            {
                await _uow.Inventory.UpdateStockAsync(
                    productId, Math.Abs(quantity),
                    TransactionType.AdjustmentOut, reference);
            }
        }

        private static string GetUnitAr(
            Models.Inventory.MaterialUnit? unit) => unit switch
            {
                Models.Inventory.MaterialUnit.Bag => "كيس",
                Models.Inventory.MaterialUnit.Ton => "طن",
                Models.Inventory.MaterialUnit.Cubic => "م³",
                Models.Inventory.MaterialUnit.Liter => "لتر",
                _ => "وحدة"
            };

        private static ProductionRecordListDto MapToListDto(
            ProductionRecord r) => new()
            {
                Id = r.Id,
                ProductionDate = r.ProductionDate,
                ProductName = r.Product?.Name ?? "-",
                ProductType = r.Product?.ProductType?.Name ?? "-",
                Shift = r.Shift == ProductionShift.Morning
                ? "صباحي" : "مسائي",
                QuantityProduced = r.QuantityProduced,
                QuantityDefective = r.QuantityDefective,
                QuantityNet = r.QuantityNet,
                Notes = r.Notes,
                MaterialUsages = r.MaterialUsages.Select(m =>
                    new MaterialUsageDto
                    {
                        MaterialName = m.RawMaterial?.Name ?? "-",
                        QuantityUsed = m.QuantityUsed,
                        Unit = GetUnitAr(m.RawMaterial?.Unit)
                    }).ToList()
            };
    }
}
