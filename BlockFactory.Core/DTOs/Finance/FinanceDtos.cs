using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.DTOs.Finance
{
    // ─── المصروفات ───────────────────────────────────
    public class ExpenseListDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public bool IsRecurring { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateExpenseDto
    {
        public Models.Finance.ExpenseCategory Category { get; set; }
        public string? CategoryOther { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; } = DateTime.Today;
        public string Description { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public bool IsRecurring { get; set; }
    }

    // ─── الحسابات ────────────────────────────────────
    public class AccountBalanceDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string TypeColor { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IsSystem { get; set; }
    }

    // ─── المحافظ الإلكترونية ─────────────────────────
    public class WalletDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
    }

    public class WalletTransferDto
    {
        public int FromWalletId { get; set; }
        public int ToWalletId { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }

    // ─── ملخص مالي ───────────────────────────────────
    public class FinancialSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }
        public decimal CashBalance { get; set; }
        public decimal TotalWalletsBalance { get; set; }
        public decimal TotalCustomerDebt { get; set; }
        public decimal TotalSupplierDebt { get; set; }
        public List<ExpenseByCategory> ExpensesByCategory { get; set; }
            = new();
        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; }
            = new();
    }

    public class ExpenseByCategory
    {
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class MonthlyRevenueDto
    {
        public string MonthName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetProfit { get; set; }
    }

    // ─── قيود اليومية ────────────────────────────────
    public class JournalEntryListDto
    {
        public int Id { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public bool IsPosted { get; set; }
        public string? Reference { get; set; }
    }

    // ─── تقرير الأرباح والخسائر ──────────────────────
    public class ProfitLossDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal NetProfit { get; set; }
        public bool IsProfit { get; set; }
        public List<RevenueItemDto> RevenueItems { get; set; } = new();
        public List<ExpenseItemDto> ExpenseItems { get; set; } = new();
    }

    public class RevenueItemDto
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class ExpenseItemDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
