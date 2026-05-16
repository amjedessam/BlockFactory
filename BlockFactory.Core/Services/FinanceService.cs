using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.Finance;
using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Finance;
using BlockFactory.Core.Models.Sales;
using BlockFactory.Core.Session;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Core.Services
{
    public class FinanceService : IFinanceService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAuthService _authService;

        public FinanceService(IUnitOfWork uow, IAuthService authService)
        {
            _uow = uow;
            _authService = authService;
        }

        // ═══════════════════════════════════════════
        // الملخص المالي
        // ═══════════════════════════════════════════

        public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(
            DateTime from, DateTime to)
        {
            var dto =   new FinancialSummaryDto();

            // الإيرادات
            dto.TotalRevenue = await _uow.Orders.Query()
                .Where(o =>
                    o.OrderDate.Date >= from.Date &&
                    o.OrderDate.Date <= to.Date &&
                    !o.IsDeleted)
                .SumAsync(o => o.PaidAmount);

            // المصروفات
            dto.TotalExpenses = await _uow.Expenses.Query()
                .Where(e =>
                    e.ExpenseDate.Date >= from.Date &&
                    e.ExpenseDate.Date <= to.Date)
                .SumAsync(e => e.Amount);

            dto.NetProfit = dto.TotalRevenue - dto.TotalExpenses;

            // الصندوق النقدي
            var cashAccount = await _uow.Accounts.Query()
                .FirstOrDefaultAsync(a => a.Code == "1001");
            dto.CashBalance = cashAccount?.Balance ?? 0;

            // المحافظ
            dto.TotalWalletsBalance = await _uow.Wallets.Query()
                .Where(w => w.IsActive)
                .SumAsync(w => w.Balance);

            // ديون العملاء
            dto.TotalCustomerDebt = await _uow.Customers.Query()
                .SumAsync(c => c.TotalDebt);

            // ديون الموردين
            dto.TotalSupplierDebt = await _uow.Suppliers.Query()
                .SumAsync(s => s.TotalDebt);

            // المصروفات بالتصنيف
            var expenseGroups = await _uow.Expenses.Query()
                .Where(e =>
                    e.ExpenseDate.Date >= from.Date &&
                    e.ExpenseDate.Date <= to.Date)
                .GroupBy(e => e.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(e => e.Amount)
                })
                .ToListAsync();

            foreach (var group in expenseGroups
                .OrderByDescending(g => g.Total))
            {
                dto.ExpensesByCategory.Add(new ExpenseByCategory
                {
                    Category = GetCategoryAr(group.Category),
                    Icon = GetCategoryIcon(group.Category),
                    Amount = group.Total,
                    Percentage = dto.TotalExpenses > 0
                        ? Math.Round(
                            (double)group.Total /
                            (double)dto.TotalExpenses * 100, 1)
                        : 0,
                    Color = GetCategoryColor(group.Category)
                });
            }

            // الإيرادات الشهرية (آخر 6 أشهر)
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.Today.AddMonths(-i);
                var mStart = new DateTime(month.Year, month.Month, 1);
                var mEnd = mStart.AddMonths(1).AddDays(-1);

                var rev = await _uow.Orders.Query()
                    .Where(o =>
                        o.OrderDate >= mStart &&
                        o.OrderDate <= mEnd)
                    .SumAsync(o => o.PaidAmount);

                var exp = await _uow.Expenses.Query()
                    .Where(e =>
                        e.ExpenseDate >= mStart &&
                        e.ExpenseDate <= mEnd)
                    .SumAsync(e => e.Amount);

                dto.MonthlyRevenue.Add(new MonthlyRevenueDto
                {
                    MonthName = mStart.ToString("MMM",
                        new System.Globalization.CultureInfo("ar-SA")),
                    Revenue = rev,
                    Expenses = exp,
                    NetProfit = rev - exp
                });
            }

            return dto;
        }

        // ═══════════════════════════════════════════
        // المصروفات
        // ═══════════════════════════════════════════

        public async Task<IEnumerable<ExpenseListDto>> GetExpensesAsync(
            DateTime from, DateTime to)
        {
            var expenses = await _uow.Expenses.Query()
                .Where(e =>
                    e.ExpenseDate.Date >= from.Date &&
                    e.ExpenseDate.Date <= to.Date)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            return expenses.Select(e => new ExpenseListDto
            {
                Id = e.Id,
                Category = GetCategoryAr(e.Category),
                CategoryIcon = GetCategoryIcon(e.Category),
                Amount = e.Amount,
                ExpenseDate = e.ExpenseDate,
                Description = e.Description,
                Reference = e.Reference,
                IsRecurring = e.IsRecurring
            });
        }

        public async Task<ServiceResult<int>> AddExpenseAsync(
            CreateExpenseDto dto)
        {
            if (dto.Amount <= 0)
                return ServiceResult<int>.Fail(
                    "المبلغ يجب أن يكون أكبر من صفر");

            if (string.IsNullOrWhiteSpace(dto.Description))
                return ServiceResult<int>.Fail("وصف المصروف مطلوب");

            var expense = new Expense
            {
                Category = dto.Category,
                CategoryOther = dto.CategoryOther,
                Amount = dto.Amount,
                ExpenseDate = dto.ExpenseDate,
                Description = dto.Description.Trim(),
                Reference = dto.Reference,
                IsRecurring = dto.IsRecurring,
                CreatedAt = DateTime.Now,
                CreatedByUserId = CurrentSession.Instance.UserId
            };

            await _uow.Expenses.AddAsync(expense);

            // تحديث رصيد حساب الصندوق
            var cashAccount = await _uow.Accounts.Query()
                .FirstOrDefaultAsync(a => a.Code == "1001");

            if (cashAccount != null)
            {
                cashAccount.Balance -= dto.Amount;
                cashAccount.UpdatedAt = DateTime.Now;
                _uow.Accounts.Update(cashAccount);
            }

            await _uow.SaveChangesAsync();

            await _authService.LogActivityAsync(
                "AddExpense", "Finance", expense.Id,
                newValues:
                    $"{GetCategoryAr(dto.Category)}: " +
                    $"{dto.Amount:N0} ر.ي");

            return ServiceResult<int>.Ok(expense.Id,
                "تم تسجيل المصروف بنجاح");
        }

        public async Task<ServiceResult> DeleteExpenseAsync(int expenseId)
        {
            var expense = await _uow.Expenses.GetByIdAsync(expenseId);
            if (expense == null)
                return ServiceResult.Fail("المصروف غير موجود");

            // فقط مصروفات اليوم يمكن حذفها
            if (expense.ExpenseDate.Date != DateTime.Today)
                return ServiceResult.Fail(
                    "لا يمكن حذف مصروفات من أيام سابقة");

            // إرجاع المبلغ للصندوق
            var cashAccount = await _uow.Accounts.Query()
                .FirstOrDefaultAsync(a => a.Code == "1001");

            if (cashAccount != null)
            {
                cashAccount.Balance += expense.Amount;
                cashAccount.UpdatedAt = DateTime.Now;
                _uow.Accounts.Update(cashAccount);
            }

            expense.IsDeleted = true;
            expense.UpdatedAt = DateTime.Now;
            _uow.Expenses.Update(expense);
            await _uow.SaveChangesAsync();

            return ServiceResult.Ok("تم حذف المصروف");
        }

        // ═══════════════════════════════════════════
        // الحسابات
        // ═══════════════════════════════════════════

        public async Task<IEnumerable<AccountBalanceDto>> GetAccountsAsync()
        {
            var accounts = await _uow.Accounts.Query()
                .Where(a => a.IsActive && !a.IsDeleted)
                .OrderBy(a => a.Code)
                .ToListAsync();

            return accounts.Select(a => new AccountBalanceDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                Type = GetAccountTypeAr(a.Type),
                TypeColor = GetAccountTypeColor(a.Type),
                Balance = a.Balance,
                IsSystem = a.IsSystem
            });
        }

        public async Task<decimal> GetAccountBalanceAsync(
            string accountCode)
        {
            var account = await _uow.Accounts.Query()
                .FirstOrDefaultAsync(a => a.Code == accountCode);
            return account?.Balance ?? 0;
        }

        // ═══════════════════════════════════════════
        // المحافظ الإلكترونية
        // ═══════════════════════════════════════════

        public async Task<IEnumerable<WalletDto>> GetWalletsAsync()
        {
            var wallets = await _uow.Wallets.Query()
                .OrderBy(w => w.Name)
                .ToListAsync();

            return wallets.Select(w => new WalletDto
            {
                Id = w.Id,
                Name = w.Name,
                AccountNumber = w.AccountNumber,
                Balance = w.Balance,
                IsActive = w.IsActive
            });
        }

        public async Task<ServiceResult> TransferBetweenWalletsAsync(
            WalletTransferDto dto)
        {
            if (dto.Amount <= 0)
                return ServiceResult.Fail(
                    "المبلغ يجب أن يكون أكبر من صفر");

            if (dto.FromWalletId == dto.ToWalletId)
                return ServiceResult.Fail(
                    "لا يمكن التحويل لنفس المحفظة");

            var fromWallet = await _uow.Wallets
                .GetByIdAsync(dto.FromWalletId);
            var toWallet = await _uow.Wallets
                .GetByIdAsync(dto.ToWalletId);

            if (fromWallet == null || toWallet == null)
                return ServiceResult.Fail("المحفظة غير موجودة");

            if (fromWallet.Balance < dto.Amount)
                return ServiceResult.Fail(
                    $"رصيد {fromWallet.Name} غير كافٍ " +
                    $"({fromWallet.Balance:N0} ر.ي)");

            await _uow.BeginTransactionAsync();
            try
            {
                fromWallet.Balance -= dto.Amount;
                fromWallet.UpdatedAt = DateTime.Now;
                _uow.Wallets.Update(fromWallet);

                toWallet.Balance += dto.Amount;
                toWallet.UpdatedAt = DateTime.Now;
                _uow.Wallets.Update(toWallet);

                // تسجيل الحركتين
                var txOut = new WalletTransaction
                {
                    WalletId = dto.FromWalletId,
                    Type = WalletTransactionType.Out,
                    Amount = dto.Amount,
                    BalanceBefore = fromWallet.Balance + dto.Amount,
                    BalanceAfter = fromWallet.Balance,
                    TransactionDate = DateTime.Now,
                    Notes = $"تحويل إلى {toWallet.Name} — {dto.Notes}",
                    CreatedAt = DateTime.Now
                };

                var txIn = new WalletTransaction
                {
                    WalletId = dto.ToWalletId,
                    Type = WalletTransactionType.In,
                    Amount = dto.Amount,
                    BalanceBefore = toWallet.Balance - dto.Amount,
                    BalanceAfter = toWallet.Balance,
                    TransactionDate = DateTime.Now,
                    Notes = $"تحويل من {fromWallet.Name} — {dto.Notes}",
                    CreatedAt = DateTime.Now
                };

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                return ServiceResult.Ok(
                    $"تم تحويل {dto.Amount:N0} ر.ي " +
                    $"من {fromWallet.Name} إلى {toWallet.Name}");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult.Fail($"خطأ: {ex.Message}");
            }
        }

        public async Task<ServiceResult> AddWalletAsync(
            string name, string? accountNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult.Fail("اسم المحفظة مطلوب");

            bool exists = await _uow.Wallets.AnyAsync(
                w => w.Name == name.Trim());

            if (exists)
                return ServiceResult.Fail("المحفظة موجودة مسبقاً");

            var wallet = new ElectronicWallet
            {
                Name = name.Trim(),
                AccountNumber = accountNumber,
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _uow.Wallets.AddAsync(wallet);
            await _uow.SaveChangesAsync();

            return ServiceResult.Ok("تم إضافة المحفظة");
        }

        // ═══════════════════════════════════════════
        // قيود اليومية
        // ═══════════════════════════════════════════

        public async Task<IEnumerable<JournalEntryListDto>>
            GetJournalEntriesAsync(DateTime from, DateTime to)
        {
            var entries = await _uow.JournalEntries.Query()
                .Where(j =>
                    j.EntryDate.Date >= from.Date &&
                    j.EntryDate.Date <= to.Date)
                .OrderByDescending(j => j.EntryDate)
                .ToListAsync();

            return entries.Select(j => new JournalEntryListDto
            {
                Id = j.Id,
                EntryNumber = j.EntryNumber,
                EntryDate = j.EntryDate,
                Type = GetJournalTypeAr(j.Type),
                Description = j.Description,
                TotalDebit = j.TotalDebit,
                TotalCredit = j.TotalCredit,
                IsPosted = j.IsPosted,
                Reference = j.Reference
            });
        }

        // ═══════════════════════════════════════════
        // تقرير الأرباح والخسائر
        // ═══════════════════════════════════════════

        public async Task<ProfitLossDto> GetProfitLossAsync(
            DateTime from, DateTime to)
        {
            // الإيرادات
            var totalSales = await _uow.Orders.Query()
                .Where(o =>
                    o.OrderDate.Date >= from.Date &&
                    o.OrderDate.Date <= to.Date)
                .SumAsync(o => o.PaidAmount);

            var deliveryRevenue = await _uow.Orders.Query()
                .Where(o =>
                    o.OrderDate.Date >= from.Date &&
                    o.OrderDate.Date <= to.Date &&
                    o.DeliveryType == DeliveryType.Delivery)
                .SumAsync(o => o.DeliveryCost);

            // المصروفات بالتصنيف
            var expenseGroups = await _uow.Expenses.Query()
                .Where(e =>
                    e.ExpenseDate.Date >= from.Date &&
                    e.ExpenseDate.Date <= to.Date)
                .GroupBy(e => e.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(e => e.Amount)
                })
                .ToListAsync();

            var totalExpenses = expenseGroups.Sum(g => g.Total);
            var totalRevenue = totalSales + deliveryRevenue;

            return new ProfitLossDto
            {
                FromDate = from,
                ToDate = to,
                TotalRevenue = totalRevenue,
                TotalExpenses = totalExpenses,
                GrossProfit = totalRevenue - totalExpenses,
                NetProfit = totalRevenue - totalExpenses,
                IsProfit = totalRevenue >= totalExpenses,

                RevenueItems = new List<RevenueItemDto>
                {
                    new RevenueItemDto
                    {
                        Description = "إيرادات المبيعات",
                        Amount = totalSales
                    },
                    new RevenueItemDto
                    {
                        Description = "إيرادات التوصيل",
                        Amount = deliveryRevenue
                    }
                },

                ExpenseItems = expenseGroups.Select(g =>
                    new ExpenseItemDto
                    {
                        Category = GetCategoryAr(g.Category),
                        Amount = g.Total
                    }).OrderByDescending(e => e.Amount).ToList()
            };
        }

        // ─── Helpers ────────────────────────────────
        private static string GetCategoryAr(ExpenseCategory c) => c switch
        {
            ExpenseCategory.Electricity => "كهرباء",
            ExpenseCategory.Maintenance => "صيانة",
            ExpenseCategory.Fuel => "وقود",
            ExpenseCategory.Transport => "نقل",
            ExpenseCategory.Stationary => "قرطاسية",
            ExpenseCategory.Other => "أخرى",
            _ => "-"
        };

        private static string GetCategoryIcon(ExpenseCategory c) => c switch
        {
            ExpenseCategory.Electricity => "⚡",
            ExpenseCategory.Maintenance => "🔧",
            ExpenseCategory.Fuel => "⛽",
            ExpenseCategory.Transport => "🚛",
            ExpenseCategory.Stationary => "📝",
            ExpenseCategory.Other => "📋",
            _ => "💰"
        };

        private static string GetCategoryColor(ExpenseCategory c) => c switch
        {
            ExpenseCategory.Electricity => "#F39C12",
            ExpenseCategory.Maintenance => "#E74C3C",
            ExpenseCategory.Fuel => "#2980B9",
            ExpenseCategory.Transport => "#27AE60",
            ExpenseCategory.Stationary => "#8E44AD",
            ExpenseCategory.Other => "#95A5A6",
            _ => "#BDC3C7"
        };

        private static string GetAccountTypeAr(AccountType t) => t switch
        {
            AccountType.Asset => "أصول",
            AccountType.Liability => "خصوم",
            AccountType.Equity => "حقوق ملكية",
            AccountType.Revenue => "إيرادات",
            AccountType.Expense => "مصروفات",
            _ => "-"
        };

        private static string GetAccountTypeColor(AccountType t) => t switch
        {
            AccountType.Asset => "#27AE60",
            AccountType.Liability => "#E74C3C",
            AccountType.Equity => "#2980B9",
            AccountType.Revenue => "#1E3A5F",
            AccountType.Expense => "#F39C12",
            _ => "#95A5A6"
        };

        private static string GetJournalTypeAr(JournalEntryType t) => t switch
        {
            JournalEntryType.Sale => "بيع",
            JournalEntryType.Purchase => "شراء",
            JournalEntryType.Payment => "دفع",
            JournalEntryType.Receipt => "استلام",
            JournalEntryType.Salary => "راتب",
            JournalEntryType.Expense => "مصروف",
            JournalEntryType.Adjustment => "تسوية",
            JournalEntryType.Opening => "افتتاحي",
            _ => "-"
        };
    }
}
