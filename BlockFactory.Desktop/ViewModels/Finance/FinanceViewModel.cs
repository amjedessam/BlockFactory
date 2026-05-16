using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.DTOs.Finance;
using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Finance;
using BlockFactory.Core.Session;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;

namespace BlockFactory.Desktop.ViewModels.Finance
{
    public class FinanceViewModel : BaseViewModel
    {
        private readonly IFinanceService _financeService;

        public FinanceViewModel(IFinanceService financeService)
        {
            _financeService = financeService;
            FromDate = new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month, 1);
            ToDate = DateTime.Today;
            InitializeCommands();
        }

        // ─── Date Range ──────────────────────────────
        private DateTime _fromDate;
        public DateTime FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        private DateTime _toDate;
        public DateTime ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        // ─── Active Tab ──────────────────────────────
        private int _activeTab;
        public int ActiveTab
        {
            get => _activeTab;
            set => SetProperty(ref _activeTab, value);
        }

        // ─── Summary ─────────────────────────────────
        private FinancialSummaryDto? _summary;
        public FinancialSummaryDto? Summary
        {
            get => _summary;
            set => SetProperty(ref _summary, value);
        }

        // ─── Expenses ────────────────────────────────
        public ObservableCollection<ExpenseListDto> Expenses { get; }
            = new();

        private ExpenseListDto? _selectedExpense;
        public ExpenseListDto? SelectedExpense
        {
            get => _selectedExpense;
            set => SetProperty(ref _selectedExpense, value);
        }

        private decimal _totalExpenses;
        public decimal TotalExpenses
        {
            get => _totalExpenses;
            set => SetProperty(ref _totalExpenses, value);
        }

        // ─── نموذج المصروف ───────────────────────────
        private bool _isExpenseFormVisible;
        public bool IsExpenseFormVisible
        {
            get => _isExpenseFormVisible;
            set => SetProperty(ref _isExpenseFormVisible, value);
        }

        private ExpenseCategory _selectedCategory =
            ExpenseCategory.Electricity;
        public ExpenseCategory SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        private decimal _expenseAmount;
        public decimal ExpenseAmount
        {
            get => _expenseAmount;
            set => SetProperty(ref _expenseAmount, value);
        }

        private string _expenseDescription = string.Empty;
        public string ExpenseDescription
        {
            get => _expenseDescription;
            set => SetProperty(ref _expenseDescription, value);
        }

        private bool _isRecurring;
        public bool IsRecurring
        {
            get => _isRecurring;
            set => SetProperty(ref _isRecurring, value);
        }

        private string? _expenseReference;
        public string? ExpenseReference
        {
            get => _expenseReference;
            set => SetProperty(ref _expenseReference, value);
        }

        // ─── Wallets ─────────────────────────────────
        public ObservableCollection<WalletDto> Wallets { get; }
            = new();

        private decimal _totalWalletsBalance;
        public decimal TotalWalletsBalance
        {
            get => _totalWalletsBalance;
            set => SetProperty(ref _totalWalletsBalance, value);
        }

        // ─── نموذج التحويل ───────────────────────────
        private bool _isTransferFormVisible;
        public bool IsTransferFormVisible
        {
            get => _isTransferFormVisible;
            set => SetProperty(ref _isTransferFormVisible, value);
        }

        private WalletDto? _transferFrom;
        public WalletDto? TransferFrom
        {
            get => _transferFrom;
            set => SetProperty(ref _transferFrom, value);
        }

        private WalletDto? _transferTo;
        public WalletDto? TransferTo
        {
            get => _transferTo;
            set => SetProperty(ref _transferTo, value);
        }

        private decimal _transferAmount;
        public decimal TransferAmount
        {
            get => _transferAmount;
            set => SetProperty(ref _transferAmount, value);
        }

        private string? _transferNotes;
        public string? TransferNotes
        {
            get => _transferNotes;
            set => SetProperty(ref _transferNotes, value);
        }

        // ─── P&L ─────────────────────────────────────
        private ProfitLossDto? _profitLoss;
        public ProfitLossDto? ProfitLoss
        {
            get => _profitLoss;
            set => SetProperty(ref _profitLoss, value);
        }

        // ─── Accounts ────────────────────────────────
        public ObservableCollection<AccountBalanceDto> Accounts { get; }
            = new();

        // ─── قوائم ───────────────────────────────────
        public List<ExpenseCategoryItem> Categories { get; } = new()
        {
            new("كهرباء ⚡", ExpenseCategory.Electricity),
            new("صيانة 🔧", ExpenseCategory.Maintenance),
            new("وقود ⛽", ExpenseCategory.Fuel),
            new("نقل 🚛", ExpenseCategory.Transport),
            new("قرطاسية 📝", ExpenseCategory.Stationary),
            new("أخرى 📋", ExpenseCategory.Other)
        };

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand LoadCommand { get; private set; } = null!;
        public RelayCommand ShowExpenseFormCommand { get; private set; }
            = null!;
        public RelayCommand CancelExpenseCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SaveExpenseCommand { get; private set; }
            = null!;
        public AsyncRelayCommand DeleteExpenseCommand { get; private set; }
            = null!;
        public RelayCommand ShowTransferFormCommand { get; private set; }
            = null!;
        public RelayCommand CancelTransferCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SaveTransferCommand { get; private set; }
            = null!;
        public AsyncRelayCommand LoadProfitLossCommand { get; private set; }
            = null!;

        public bool CanDeleteExpense =>
            CurrentSession.Instance.HasPermission("DeleteExpense");

        private void InitializeCommands()
        {
            LoadCommand = new AsyncRelayCommand(
                async _ => await LoadAsync());

            ShowExpenseFormCommand = new RelayCommand(_ =>
            {
                ClearExpenseForm();
                IsExpenseFormVisible = true;
            });

            CancelExpenseCommand = new RelayCommand(_ =>
            {
                IsExpenseFormVisible = false;
                ClearMessages();
            });

            SaveExpenseCommand = new AsyncRelayCommand(
                async _ => await SaveExpenseAsync(),
                _ => ExpenseAmount > 0 &&
                     !string.IsNullOrWhiteSpace(ExpenseDescription));

            DeleteExpenseCommand = new AsyncRelayCommand(
                async _ => await DeleteExpenseAsync(),
                _ => SelectedExpense != null &&
                     CurrentSession.Instance.HasPermission("DeleteExpense"));

            ShowTransferFormCommand = new RelayCommand(_ =>
            {
                TransferAmount = 0;
                TransferNotes = null;
                IsTransferFormVisible = true;
            });

            CancelTransferCommand = new RelayCommand(_ =>
            {
                IsTransferFormVisible = false;
                ClearMessages();
            });

            SaveTransferCommand = new AsyncRelayCommand(
                async _ => await SaveTransferAsync(),
                _ => TransferFrom != null &&
                     TransferTo != null &&
                     TransferAmount > 0);

            LoadProfitLossCommand = new AsyncRelayCommand(
                async _ => await LoadProfitLossAsync());
        }

        // ─── Load ────────────────────────────────────
        public async Task LoadAsync()
        {
            try
            {
                IsLoading = true;

                // الملخص المالي
                Summary = await _financeService
                    .GetFinancialSummaryAsync(FromDate, ToDate);

                // المصروفات
                var expenses = await _financeService
                    .GetExpensesAsync(FromDate, ToDate);
                Expenses.Clear();
                foreach (var e in expenses)
                    Expenses.Add(e);
                TotalExpenses = Expenses.Sum(e => e.Amount);

                // المحافظ
                var wallets = await _financeService.GetWalletsAsync();
                Wallets.Clear();
                foreach (var w in wallets)
                    Wallets.Add(w);
                TotalWalletsBalance = Wallets
                    .Where(w => w.IsActive)
                    .Sum(w => w.Balance);

                // الحسابات
                var accounts = await _financeService.GetAccountsAsync();
                Accounts.Clear();
                foreach (var a in accounts)
                    Accounts.Add(a);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadProfitLossAsync()
        {
            try
            {
                IsLoading = true;
                ProfitLoss = await _financeService
                    .GetProfitLossAsync(FromDate, ToDate);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ─── Save Expense ────────────────────────────
        private async Task SaveExpenseAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                var result = await _financeService.AddExpenseAsync(
                    new CreateExpenseDto
                    {
                        Category = SelectedCategory,
                        Amount = ExpenseAmount,
                        ExpenseDate = DateTime.Today,
                        Description = ExpenseDescription,
                        Reference = ExpenseReference,
                        IsRecurring = IsRecurring
                    });

                if (result.Success)
                {
                    ShowSuccess(result.Message);
                    IsExpenseFormVisible = false;
                    ClearExpenseForm();
                    await LoadAsync();
                }
                else ShowError(result.Message);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteExpenseAsync()
        {
            if (SelectedExpense == null) return;

            var confirm = MessageBox.Show(
                $"هل تريد حذف المصروف:\n" +
                $"{SelectedExpense.Description}\n" +
                $"المبلغ: {SelectedExpense.Amount:N0} ر.ي؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);

            if (confirm != MessageBoxResult.Yes) return;

            var result = await _financeService
                .DeleteExpenseAsync(SelectedExpense.Id);

            if (result.Success)
            {
                ShowSuccess(result.Message);
                await LoadAsync();
            }
            else ShowError(result.Message);
        }

        // ─── Save Transfer ───────────────────────────
        private async Task SaveTransferAsync()
        {
            if (TransferFrom == null || TransferTo == null) return;

            var result = await _financeService
                .TransferBetweenWalletsAsync(new WalletTransferDto
                {
                    FromWalletId = TransferFrom.Id,
                    ToWalletId = TransferTo.Id,
                    Amount = TransferAmount,
                    Notes = TransferNotes
                });

            if (result.Success)
            {
                ShowSuccess(result.Message);
                IsTransferFormVisible = false;
                await LoadAsync();
            }
            else ShowError(result.Message);
        }

        private void ClearExpenseForm()
        {
            SelectedCategory = ExpenseCategory.Electricity;
            ExpenseAmount = 0;
            ExpenseDescription = string.Empty;
            ExpenseReference = null;
            IsRecurring = false;
            ClearMessages();
        }
    }

    public record ExpenseCategoryItem(
        string Name, ExpenseCategory Value);
}
