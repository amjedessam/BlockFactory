using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.DTOs.Suppliers;
using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Inventory;
using BlockFactory.Core.Models.Suppliers;
using BlockFactory.Core.Session;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Core.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAuthService _authService;

        public SupplierService(IUnitOfWork uow, IAuthService authService)
        {
            _uow = uow;
            _authService = authService;
        }

        // ─── ملخص الموردين ───────────────────────────
        public async Task<SuppliersSummaryDto> GetSummaryAsync()
        {
            var monthStart = new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month, 1);

            var totalDebt = await _uow.Suppliers.Query()
                .SumAsync(s => s.TotalDebt);

            var suppliersWithDebt = await _uow.Suppliers.Query()
                .CountAsync(s => s.TotalDebt > 0);

            var monthPurchases = await _uow.SupplierInvoices.Query()
                .Where(i => i.InvoiceDate >= monthStart)
                .SumAsync(i => i.TotalAmount);

            return new SuppliersSummaryDto
            {
                TotalSuppliers = await _uow.Suppliers.Query().CountAsync(),
                TotalDebt = totalDebt,
                SuppliersWithDebt = suppliersWithDebt,
                TotalPurchasesThisMonth = monthPurchases
            };
        }

        // ─── قائمة الموردين ─────────────────────────
        public async Task<IEnumerable<SupplierListDto>> GetAllSuppliersAsync()
        {
            var suppliers = await _uow.Suppliers.Query()
                .Include(s => s.Invoices)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            return suppliers.Select(MapToListDto);
        }

        public async Task<IEnumerable<SupplierListDto>> SearchSuppliersAsync(
            string keyword)
        {
            keyword = keyword.ToLower().Trim();

            var suppliers = await _uow.Suppliers.Query()
                .Include(s => s.Invoices)
                .Where(s =>
                    s.FullName.ToLower().Contains(keyword) ||
                    (s.CompanyName != null &&
                     s.CompanyName.ToLower().Contains(keyword)) ||
                    (s.Phone != null && s.Phone.Contains(keyword)))
                .OrderBy(s => s.FullName)
                .ToListAsync();

            return suppliers.Select(MapToListDto);
        }

        public async Task<SupplierDetailDto?> GetSupplierDetailAsync(
            int supplierId)
        {
            var supplier = await _uow.Suppliers.Query()
                .Include(s => s.Invoices)
                    .ThenInclude(i => i.Items)
                        .ThenInclude(it => it.RawMaterial)
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == supplierId);

            if (supplier == null) return null;

            return new SupplierDetailDto
            {
                Id = supplier.Id,
                FullName = supplier.FullName,
                CompanyName = supplier.CompanyName,
                Phone = supplier.Phone,
                Address = supplier.Address,
                SupplierType = GetTypeAr(supplier.SupplierType),
                TotalDebt = supplier.TotalDebt,
                Notes = supplier.Notes,

                RecentInvoices = supplier.Invoices
                    .OrderByDescending(i => i.InvoiceDate)
                    .Take(10)
                    .Select(MapToInvoiceDto).ToList(),

                RecentPayments = supplier.Payments
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(10)
                    .Select(p => new SupplierPaymentDto
                    {
                        Id = p.Id,
                        Amount = p.Amount,
                        PaymentDate = p.PaymentDate,
                        Method = p.Method,
                        Reference = p.Reference,
                        Notes = p.Notes
                    }).ToList()
            };
        }

        // ─── إنشاء مورد ─────────────────────────────
        public async Task<ServiceResult<int>> CreateSupplierAsync(
            CreateSupplierDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                return ServiceResult<int>.Fail("اسم المورد مطلوب");

            var supplier = new Supplier
            {
                FullName = dto.FullName.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Phone = dto.Phone?.Trim(),
                Address = dto.Address?.Trim(),
                SupplierType = dto.SupplierType,
                TotalDebt = 0,
                Notes = dto.Notes,
                CreatedAt = DateTime.Now,
                CreatedByUserId = CurrentSession.Instance.UserId
            };

            await _uow.Suppliers.AddAsync(supplier);
            await _uow.SaveChangesAsync();

            await _authService.LogActivityAsync(
                "CreateSupplier", "Supplier", supplier.Id,
                newValues: supplier.FullName);

            return ServiceResult<int>.Ok(supplier.Id,
                "تم إضافة المورد بنجاح");
        }

        // ─── تحديث مورد ─────────────────────────────
        public async Task<ServiceResult> UpdateSupplierAsync(
            int id, CreateSupplierDto dto)
        {
            var supplier = await _uow.Suppliers.GetByIdAsync(id);
            if (supplier == null)
                return ServiceResult.Fail("المورد غير موجود");

            supplier.FullName = dto.FullName.Trim();
            supplier.CompanyName = dto.CompanyName?.Trim();
            supplier.Phone = dto.Phone?.Trim();
            supplier.Address = dto.Address?.Trim();
            supplier.SupplierType = dto.SupplierType;
            supplier.Notes = dto.Notes;
            supplier.UpdatedAt = DateTime.Now;

            _uow.Suppliers.Update(supplier);
            await _uow.SaveChangesAsync();

            return ServiceResult.Ok("تم تحديث بيانات المورد");
        }

        // ─── فواتير المورد ───────────────────────────
        public async Task<IEnumerable<SupplierInvoiceDto>>
            GetSupplierInvoicesAsync(int supplierId)
        {
            var invoices = await _uow.SupplierInvoices.Query()
                .Include(i => i.Items)
                    .ThenInclude(it => it.RawMaterial)
                .Where(i => i.SupplierId == supplierId)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            return invoices.Select(MapToInvoiceDto);
        }

        public async Task<IReadOnlyList<RawMaterialLookupDto>>
            GetActiveRawMaterialsAsync()
        {
            var materials = await _uow.RawMaterials.Query()
                .Where(m => m.IsActive && !m.IsDeleted)
                .OrderBy(m => m.Name)
                .ToListAsync();

            return materials
                .Select(m => new RawMaterialLookupDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    UnitAr = MaterialUnitToAr(m.Unit)
                })
                .ToList();
        }

        // ─── إنشاء فاتورة مورد ──────────────────────
        public async Task<ServiceResult<int>> CreateInvoiceAsync(
            CreateSupplierInvoiceDto dto)
        {
            if (!dto.Items.Any())
                return ServiceResult<int>.Fail(
                    "يجب إضافة عنصر واحد على الأقل");

            var supplier = await _uow.Suppliers.GetByIdAsync(dto.SupplierId);
            if (supplier == null)
                return ServiceResult<int>.Fail("المورد غير موجود");

            var totalAmount = dto.Items
                .Sum(i => i.Quantity * i.UnitPrice);

            if (!dto.IsCredit)
            {
                if (dto.PayNowAmount <= 0)
                    return ServiceResult<int>.Fail(
                        "عند اختيار الدفع الآن يجب إدخال مبلغ مدفوع أكبر من صفر");

                if (dto.PayNowAmount > totalAmount)
                    return ServiceResult<int>.Fail(
                        "مبلغ الدفع الآن لا يمكن أن يتجاوز إجمالي الفاتورة");
            }

            await _uow.BeginTransactionAsync();
            try
            {
                var paidNow = 0m;
                if (!dto.IsCredit)
                    paidNow = dto.PayNowAmount;

                var remaining = totalAmount - paidNow;
                var status = remaining <= 0
                    ? SupplierInvoiceStatus.FullyPaid
                    : paidNow > 0
                        ? SupplierInvoiceStatus.PartiallyPaid
                        : SupplierInvoiceStatus.Unpaid;

                var invoice = new SupplierInvoice
                {
                    SupplierId = dto.SupplierId,
                    InvoiceNumber = dto.InvoiceNumber.Trim(),
                    InvoiceDate = dto.InvoiceDate,
                    DueDate = dto.IsCredit ? dto.DueDate : null,
                    TotalAmount = totalAmount,
                    PaidAmount = paidNow,
                    RemainingAmount = remaining,
                    Status = status,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = CurrentSession.Instance.UserId
                };

                await _uow.SupplierInvoices.AddAsync(invoice);
                await _uow.SaveChangesAsync();

                // إضافة عناصر الفاتورة
                foreach (var item in dto.Items)
                {
                    var invoiceItem = new SupplierInvoiceItem
                    {
                        SupplierInvoiceId = invoice.Id,
                        RawMaterialId = item.RawMaterialId,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.Quantity * item.UnitPrice,
                        CreatedAt = DateTime.Now
                    };
                    await _uow.SupplierInvoiceItems.AddAsync(invoiceItem);
                }

                // زيادة مخزون المواد الخام لكل بند مرتبط بمادة
                foreach (var item in dto.Items)
                {
                    if (!item.RawMaterialId.HasValue ||
                        item.Quantity <= 0)
                        continue;

                    var material = await _uow.RawMaterials
                        .GetByIdAsync(item.RawMaterialId.Value);

                    if (material == null)
                        continue;

                    var before = material.QuantityAvailable;
                    material.QuantityAvailable += item.Quantity;
                    material.UpdatedAt = DateTime.Now;
                    _uow.RawMaterials.Update(material);

                    var rawTrans = new RawMaterialTransaction
                    {
                        RawMaterialId = material.Id,
                        Type = RawMaterialTransactionType.PurchaseIn,
                        Quantity = item.Quantity,
                        QuantityBefore = before,
                        QuantityAfter = material.QuantityAvailable,
                        UnitCost = item.UnitPrice,
                        TotalCost = item.Quantity * item.UnitPrice,
                        TransactionDate = DateTime.Now,
                        Reference = dto.InvoiceNumber.Trim(),
                        Notes = item.Description,
                        SupplierId = dto.SupplierId,
                        CreatedAt = DateTime.Now
                    };
                    await _uow.RawMaterialTransactions.AddAsync(rawTrans);
                }

                // تحديث دين المورد: يزيد فقط بما تبقّى غير مدفوع على الفاتورة
                supplier.TotalDebt += remaining;
                supplier.UpdatedAt = DateTime.Now;
                _uow.Suppliers.Update(supplier);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                var payNote = dto.IsCredit
                    ? "آجل"
                    : $"دفع الآن {paidNow:N0} ر.ي — متبقي {remaining:N0} ر.ي";

                await _authService.LogActivityAsync(
                    "CreateSupplierInvoice",
                    "Supplier", dto.SupplierId,
                    newValues:
                        $"فاتورة: {dto.InvoiceNumber} " +
                        $"— {totalAmount:N0} ر.ي ({payNote})");

                var msg = dto.IsCredit
                    ? $"تم إضافة الفاتورة (آجل) — إجمالي: {totalAmount:N0} ر.ي"
                    : remaining <= 0
                        ? $"تم إضافة الفاتورة (مدفوعة) — {totalAmount:N0} ر.ي"
                        : $"تم إضافة الفاتورة — إجمالي: {totalAmount:N0} ر.ي، " +
                          $"مدفوع: {paidNow:N0} ر.ي، متبقي على المورد: {remaining:N0} ر.ي";

                return ServiceResult<int>.Ok(invoice.Id, msg);
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult<int>.Fail($"خطأ: {ex.Message}");
            }
        }

        // ─── دفع للمورد ─────────────────────────────
        public async Task<ServiceResult> PaySupplierAsync(
            PaySupplierDto dto)
        {
            if (dto.Amount <= 0)
                return ServiceResult.Fail(
                    "المبلغ يجب أن يكون أكبر من صفر");

            var supplier = await _uow.Suppliers.GetByIdAsync(dto.SupplierId);
            if (supplier == null)
                return ServiceResult.Fail("المورد غير موجود");

            if (dto.Amount > supplier.TotalDebt)
                return ServiceResult.Fail(
                    $"المبلغ أكبر من الدين " +
                    $"({supplier.TotalDebt:N0} ر.ي)");

            await _uow.BeginTransactionAsync();
            try
            {
                // تسجيل الدفعة
                var payment = new SupplierPayment
                {
                    SupplierId = dto.SupplierId,
                    SupplierInvoiceId = dto.InvoiceId,
                    Amount = dto.Amount,
                    PaymentDate = DateTime.Now,
                    Method = dto.Method,
                    Reference = dto.Reference,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = CurrentSession.Instance.UserId
                };
                await _uow.SupplierPayments.AddAsync(payment);

                // تحديث دين المورد
                supplier.TotalDebt -= dto.Amount;
                if (supplier.TotalDebt < 0) supplier.TotalDebt = 0;
                supplier.UpdatedAt = DateTime.Now;
                _uow.Suppliers.Update(supplier);

                // تحديث الفاتورة إن وجدت
                if (dto.InvoiceId.HasValue)
                {
                    var invoice = await _uow.SupplierInvoices
                        .GetByIdAsync(dto.InvoiceId.Value);

                    if (invoice != null)
                    {
                        invoice.PaidAmount += dto.Amount;
                        invoice.RemainingAmount -= dto.Amount;
                        invoice.Status = invoice.RemainingAmount <= 0
                            ? SupplierInvoiceStatus.FullyPaid
                            : SupplierInvoiceStatus.PartiallyPaid;
                        invoice.UpdatedAt = DateTime.Now;
                        _uow.SupplierInvoices.Update(invoice);
                    }
                }

                // تسجيل مصروف
                var expense = new Models.Finance.Expense
                {
                    Category = Models.Finance.ExpenseCategory.Other,
                    Amount = dto.Amount,
                    ExpenseDate = DateTime.Now,
                    Description =
                        $"دفع للمورد: {supplier.FullName}",
                    Notes = dto.Notes,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = CurrentSession.Instance.UserId
                };
                await _uow.Expenses.AddAsync(expense);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                await _authService.LogActivityAsync(
                    "PaySupplier", "Supplier", dto.SupplierId,
                    newValues: $"دفعة: {dto.Amount:N0} ر.ي");

                return ServiceResult.Ok(
                    $"تم تسجيل دفعة {dto.Amount:N0} ر.ي " +
                    $"للمورد {supplier.FullName}");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult.Fail($"خطأ: {ex.Message}");
            }
        }

        public async Task<IEnumerable<SupplierListDto>>
            GetSuppliersWithDebtAsync()
        {
            var suppliers = await _uow.Suppliers.Query()
                .Include(s => s.Invoices)
                .Where(s => s.TotalDebt > 0)
                .OrderByDescending(s => s.TotalDebt)
                .ToListAsync();

            return suppliers.Select(MapToListDto);
        }

        // ─── Helpers ────────────────────────────────
        private static SupplierListDto MapToListDto(Supplier s) => new()
        {
            Id = s.Id,
            FullName = s.FullName,
            CompanyName = s.CompanyName,
            Phone = s.Phone,
            SupplierType = GetTypeAr(s.SupplierType),
            TypeIcon = GetTypeIcon(s.SupplierType),
            TotalDebt = s.TotalDebt,
            DebtStatusColor = s.TotalDebt <= 0
                ? "#27AE60"
                : s.TotalDebt < 500000
                    ? "#F39C12"
                    : "#E74C3C",
            TotalInvoices = s.Invoices?.Count ?? 0,
            TotalPurchases = s.Invoices?.Sum(i => i.TotalAmount) ?? 0,
            CreatedAt = s.CreatedAt
        };

        private static SupplierInvoiceDto MapToInvoiceDto(
            SupplierInvoice i) => new()
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                TotalAmount = i.TotalAmount,
                PaidAmount = i.PaidAmount,
                RemainingAmount = i.RemainingAmount,
                Status = GetInvoiceStatusAr(i.Status),
                StatusColor = GetInvoiceStatusColor(i.Status),
                Items = i.Items?.Select(it => new SupplierInvoiceItemDto
                {
                    Description = it.Description,
                    MaterialName = it.RawMaterial?.Name ?? it.Description ?? "-",
                    Quantity = it.Quantity,
                    Unit = it.Unit,
                    UnitPrice = it.UnitPrice,
                    TotalPrice = it.TotalPrice
                }).ToList() ?? new()
            };

        private static string GetTypeAr(
            Models.Suppliers.SupplierType t) => t switch
            {
                Models.Suppliers.SupplierType.Cement => "إسمنت",
                Models.Suppliers.SupplierType.Sand => "رمل",
                Models.Suppliers.SupplierType.Gravel => "حصى",
                Models.Suppliers.SupplierType.Water => "ماء",
                Models.Suppliers.SupplierType.Electricity => "كهرباء",
                Models.Suppliers.SupplierType.Other => "أخرى",
                _ => "-"
            };

        private static string GetTypeIcon(
            Models.Suppliers.SupplierType t) => t switch
            {
                Models.Suppliers.SupplierType.Cement => "🏭",
                Models.Suppliers.SupplierType.Sand => "🏖️",
                Models.Suppliers.SupplierType.Gravel => "🪨",
                Models.Suppliers.SupplierType.Water => "💧",
                Models.Suppliers.SupplierType.Electricity => "⚡",
                Models.Suppliers.SupplierType.Other => "📦",
                _ => "🏢"
            };

        private static string GetInvoiceStatusAr(
            SupplierInvoiceStatus s) => s switch
            {
                SupplierInvoiceStatus.Unpaid => "غير مسددة",
                SupplierInvoiceStatus.PartiallyPaid => "جزئي",
                SupplierInvoiceStatus.FullyPaid => "مسددة",
                _ => "-"
            };

        private static string GetInvoiceStatusColor(
            SupplierInvoiceStatus s) => s switch
            {
                SupplierInvoiceStatus.Unpaid => "#E74C3C",
                SupplierInvoiceStatus.PartiallyPaid => "#F39C12",
                SupplierInvoiceStatus.FullyPaid => "#27AE60",
                _ => "#95A5A6"
            };

        private static string MaterialUnitToAr(MaterialUnit u)
            => u switch
            {
                MaterialUnit.Bag => "كيس",
                MaterialUnit.Ton => "طن",
                MaterialUnit.Cubic => "م³",
                MaterialUnit.Liter => "لتر",
                _ => "وحدة"
            };
    }
}
