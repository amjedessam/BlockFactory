using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.Finance;
using BlockFactory.Core.DTOs.Orders;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface IFinanceService
    {
        // ─── ملخص مالي ──────────────────────────────
        Task<FinancialSummaryDto> GetFinancialSummaryAsync(
            DateTime from, DateTime to);

        // ─── المصروفات ──────────────────────────────
        Task<IEnumerable<ExpenseListDto>> GetExpensesAsync(
            DateTime from, DateTime to);
        Task<ServiceResult<int>> AddExpenseAsync(CreateExpenseDto dto);
        Task<ServiceResult> DeleteExpenseAsync(int expenseId);

        // ─── الحسابات ───────────────────────────────
        Task<IEnumerable<AccountBalanceDto>> GetAccountsAsync();
        Task<decimal> GetAccountBalanceAsync(string accountCode);

        // ─── المحافظ ────────────────────────────────
        Task<IEnumerable<WalletDto>> GetWalletsAsync();
        Task<ServiceResult> TransferBetweenWalletsAsync(
            WalletTransferDto dto);
        Task<ServiceResult> AddWalletAsync(string name,
            string? accountNumber);

        // ─── قيود اليومية ───────────────────────────
        Task<IEnumerable<JournalEntryListDto>> GetJournalEntriesAsync(
            DateTime from, DateTime to);

        // ─── تقارير ─────────────────────────────────
        Task<ProfitLossDto> GetProfitLossAsync(
            DateTime from, DateTime to);
    }
}
