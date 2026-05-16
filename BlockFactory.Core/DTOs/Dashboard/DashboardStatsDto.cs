using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        // ─── المبيعات ───────────────────────────────
        public decimal TodaySales { get; set; }
        public decimal MonthSales { get; set; }
        public int TodayOrdersCount { get; set; }
        public int MonthOrdersCount { get; set; }

        // ─── الديون ────────────────────────────────
        public decimal TotalCustomerDebt { get; set; }
        public int CustomersWithDebtCount { get; set; }
        public int PledgesDueSoonCount { get; set; }

        // ─── الإنتاج ───────────────────────────────
        public int TodayProduction { get; set; }
        public int MonthProduction { get; set; }

        // ─── المخزون ───────────────────────────────
        public int LowStockProductsCount { get; set; }
        public int LowRawMaterialsCount { get; set; }

        // ─── المالية ───────────────────────────────
        public decimal CashBalance { get; set; }
        public decimal TotalWalletsBalance { get; set; }
        public decimal MonthExpenses { get; set; }
        public decimal NetProfit { get; set; }
    }

    public class RecentOrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
    }

    public class AlertDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public AlertType Type { get; set; }
    }

    public enum AlertType
    {
        Warning,
        Danger,
        Info
    }
}
