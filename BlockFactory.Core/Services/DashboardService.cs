using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.DTOs.Dashboard;
using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Finance;
using BlockFactory.Core.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Core.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _uow;

        public DashboardService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ─── الإحصائيات الرئيسية ────────────────────
        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var dto = new DashboardStatsDto();

            // مبيعات اليوم
            var todayOrders = await _uow.Orders.Query()
                .Where(o => o.OrderDate.Date == today &&
                            !o.IsDeleted)
                .ToListAsync();

            dto.TodaySales = todayOrders.Sum(o => o.TotalAmount);
            dto.TodayOrdersCount = todayOrders.Count;

            // مبيعات الشهر
            var monthOrders = await _uow.Orders.Query()
                .Where(o => o.OrderDate >= monthStart &&
                            !o.IsDeleted)
                .ToListAsync();

            dto.MonthSales = monthOrders.Sum(o => o.TotalAmount);
            dto.MonthOrdersCount = monthOrders.Count;

            // ديون العملاء
            dto.TotalCustomerDebt = await _uow.Customers.Query()
                .Where(c => !c.IsDeleted)
                .SumAsync(c => c.TotalDebt);

            dto.CustomersWithDebtCount = await _uow.Customers.Query()
                .CountAsync(c => c.TotalDebt > 0 && !c.IsDeleted);

            // رهون قريبة الاستحقاق (7 أيام)
            var cutoff = today.AddDays(7);
            dto.PledgesDueSoonCount = await _uow.Pledges.Query()
                .CountAsync(p =>
                    p.Status == Models.Customers.PledgeStatus.Active &&
                    p.DueDate <= cutoff);

            // إنتاج اليوم
            dto.TodayProduction = await _uow.Productions.Query()
                .Where(p => p.ProductionDate.Date == today)
                .SumAsync(p => p.QuantityNet);

            // إنتاج الشهر
            dto.MonthProduction = await _uow.Productions.Query()
                .Where(p => p.ProductionDate >= monthStart)
                .SumAsync(p => p.QuantityNet);

            // مخزون منخفض
            dto.LowStockProductsCount = await _uow.Inventory.Query()
                .CountAsync(s =>
                    s.QuantityAvailable <= s.MinimumThreshold);

            dto.LowRawMaterialsCount = await _uow.RawMaterials.Query()
                .CountAsync(m =>
                    m.QuantityAvailable <= m.MinimumThreshold &&
                    m.IsActive);

            // الصندوق النقدي
            var cashAccount = await _uow.Accounts.Query()
                .FirstOrDefaultAsync(a => a.Code == "1001");
            dto.CashBalance = cashAccount?.Balance ?? 0;

            // إجمالي المحافظ
            dto.TotalWalletsBalance = await _uow.Wallets.Query()
                .Where(w => w.IsActive)
                .SumAsync(w => w.Balance);

            // مصروفات الشهر
            dto.MonthExpenses = await _uow.Expenses.Query()
                .Where(e => e.ExpenseDate >= monthStart)
                .SumAsync(e => e.Amount);

            // صافي الربح
            dto.NetProfit = dto.MonthSales - dto.MonthExpenses;

            return dto;
        }

        // ─── آخر الطلبات ────────────────────────────
        public async Task<IEnumerable<RecentOrderDto>> GetRecentOrdersAsync(
            int count = 8)
        {
            var orders = await _uow.Orders.Query()
                .Include(o => o.Customer)
                .Where(o => !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .Take(count)
                .ToListAsync();

            return orders.Select(o => new RecentOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.Customer?.FullName ?? "-",
                TotalAmount = o.TotalAmount,
                PaymentType = GetPaymentTypeAr(o.PaymentType),
                Status = GetStatusAr(o.PaymentStatus),
                StatusColor = GetStatusColor(o.PaymentStatus),
                OrderDate = o.OrderDate
            });
        }

        // ─── التنبيهات ──────────────────────────────
        public async Task<IEnumerable<AlertDto>> GetAlertsAsync()
        {
            var alerts = new List<AlertDto>();
            var today = DateTime.Today;

            // رهون منتهية الصلاحية
            var expiredPledges = await _uow.Pledges.Query()
                .Include(p => p.Customer)
                .Where(p =>
                    p.Status == Models.Customers.PledgeStatus.Active &&
                    p.DueDate.Date < today)
                .CountAsync();

            if (expiredPledges > 0)
                alerts.Add(new AlertDto
                {
                    Title = "رهون منتهية الصلاحية",
                    Message = $"يوجد {expiredPledges} رهن تجاوز موعد السداد",
                    Icon = "⚠️",
                    Color = "#E74C3C",
                    Type = AlertType.Danger
                });

            // رهون تستحق خلال 3 أيام
            var urgentPledges = await _uow.Pledges.Query()
                .Where(p =>
                    p.Status == Models.Customers.PledgeStatus.Active &&
                    p.DueDate.Date >= today &&
                    p.DueDate.Date <= today.AddDays(3))
                .CountAsync();

            if (urgentPledges > 0)
                alerts.Add(new AlertDto
                {
                    Title = "رهون تستحق قريباً",
                    Message = $"{urgentPledges} رهن يستحق خلال 3 أيام",
                    Icon = "🔔",
                    Color = "#E67E22",
                    Type = AlertType.Warning
                });

            // مخزون منخفض
            var lowStock = await _uow.Inventory.Query()
                .Include(s => s.Product)
                .Where(s => s.QuantityAvailable <= s.MinimumThreshold)
                .ToListAsync();

            foreach (var stock in lowStock.Take(3))
                alerts.Add(new AlertDto
                {
                    Title = "مخزون منخفض",
                    Message = $"{stock.Product?.Name}: " +
                              $"متبقي {stock.QuantityAvailable} قطعة",
                    Icon = "📦",
                    Color = "#F39C12",
                    Type = AlertType.Warning
                });

            // مواد خام منخفضة
            var lowMaterials = await _uow.RawMaterials.Query()
                .Where(m =>
                    m.IsActive &&
                    m.QuantityAvailable <= m.MinimumThreshold)
                .ToListAsync();

            foreach (var mat in lowMaterials)
                alerts.Add(new AlertDto
                {
                    Title = "مادة خام منخفضة",
                    Message = $"{mat.Name}: متبقي {mat.QuantityAvailable}",
                    Icon = "🪨",
                    Color = "#8E44AD",
                    Type = AlertType.Warning
                });

            return alerts;
        }

        // ─── Helpers ────────────────────────────────
        private static string GetPaymentTypeAr(PaymentType type) => type switch
        {
            PaymentType.Cash => "نقد",
            PaymentType.Electronic => "تحويل",
            PaymentType.Credit => "آجل",
            PaymentType.Pledge => "رهن",
            PaymentType.Mixed => "مختلط",
            _ => "-"
        };

        private static string GetStatusAr(PaymentStatus status) => status switch
        {
            PaymentStatus.FullyPaid => "مسدد",
            PaymentStatus.PartiallyPaid => "جزئي",
            PaymentStatus.Unpaid => "غير مسدد",
            _ => "-"
        };

        private static string GetStatusColor(PaymentStatus status) => status switch
        {
            PaymentStatus.FullyPaid => "#27AE60",
            PaymentStatus.PartiallyPaid => "#F39C12",
            PaymentStatus.Unpaid => "#E74C3C",
            _ => "#95A5A6"
        };
    }
}
