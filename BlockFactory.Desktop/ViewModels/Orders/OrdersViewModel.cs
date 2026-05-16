using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Sales;
using BlockFactory.Core.Session;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;

namespace BlockFactory.Desktop.ViewModels.Orders
{
    public class OrdersViewModel : BaseViewModel
    {
        private readonly IOrderService _orderService;
        private readonly IReportService _reportService;
        private readonly INavigationService _navigation;

        public OrdersViewModel(
            IOrderService orderService,
            IReportService reportService,
            INavigationService navigation)
        {
            _orderService = orderService;
            _reportService = reportService;
            _navigation = navigation;
            InitializeCommands();
        }

        public bool CanDeleteOrder =>
            CurrentSession.Instance.HasPermission("DeleteOrder");

        // ─── Collections ────────────────────────────
        public ObservableCollection<OrderListDto> Orders { get; }
            = new();

        private ObservableCollection<OrderListDto> _allOrders = new();

        // ─── Properties ────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterOrders();
            }
        }

        private string _selectedFilter = "الكل";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                SetProperty(ref _selectedFilter, value);
                FilterOrders();
            }
        }

        private DateTime _fromDate = DateTime.Today.AddDays(-30);
        public DateTime FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        private DateTime _toDate = DateTime.Today;
        public DateTime ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        private OrderListDto? _selectedOrder;
        public OrderListDto? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (!Equals(_selectedOrder, value))
                {
                    IsPaymentFormVisible = false;
                    SetProperty(ref _selectedOrder, value);
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // لوحة تسجيل دفعة
        private bool _isPaymentFormVisible;
        public bool IsPaymentFormVisible
        {
            get => _isPaymentFormVisible;
            set => SetProperty(ref _isPaymentFormVisible, value);
        }

        private decimal _payAmount;
        public decimal PayAmount
        {
            get => _payAmount;
            set
            {
                if (SetProperty(ref _payAmount, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool _payAsElectronic = true;
        public bool PayAsElectronic
        {
            get => _payAsElectronic;
            set
            {
                if (SetProperty(ref _payAsElectronic, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private string? _payWallet;
        public string? PayWallet
        {
            get => _payWallet;
            set
            {
                if (SetProperty(ref _payWallet, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private string? _payReference;
        public string? PayReference
        {
            get => _payReference;
            set => SetProperty(ref _payReference, value);
        }

        private string? _payNotes;
        public string? PayNotes
        {
            get => _payNotes;
            set => SetProperty(ref _payNotes, value);
        }

        public List<string> WalletOptions { get; } = new()
        {
            "سبأفون",
            "وان كاش"
        };

        // إجماليات
        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        private decimal _totalPaid;
        public decimal TotalPaid
        {
            get => _totalPaid;
            set => SetProperty(ref _totalPaid, value);
        }

        private decimal _totalRemaining;
        public decimal TotalRemaining
        {
            get => _totalRemaining;
            set => SetProperty(ref _totalRemaining, value);
        }

        private int _ordersCount;
        public int OrdersCount
        {
            get => _ordersCount;
            set => SetProperty(ref _ordersCount, value);
        }

        // فلاتر الحالة
        public List<string> FilterOptions { get; } = new()
        {
            "الكل",
            "مسدد",
            "جزئي",
            "غير مسدد",
            "متأخر",
            "رهن"
        };

        // ─── Commands ───────────────────────────────
        public RelayCommand FilterCommand { get; private set; } = null!;
        public AsyncRelayCommand LoadOrdersCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SearchCommand { get; private set; }
            = null!;
        public RelayCommand NewOrderCommand { get; private set; }
            = null!;
        public RelayCommand ViewOrderCommand { get; private set; }
            = null!;
        public AsyncRelayCommand AddPaymentCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SavePaymentCommand { get; private set; }
            = null!;
        public RelayCommand CancelPaymentCommand { get; private set; }
            = null!;
        public AsyncRelayCommand PrintInvoiceCommand { get; private set; }
            = null!;
        public AsyncRelayCommand CancelOrderCommand { get; private set; }
            = null!;

        private void InitializeCommands()
        {
            FilterCommand = new RelayCommand(param =>
            {
                if (param is string s)
                    SelectedFilter = s;
            });

            LoadOrdersCommand = new AsyncRelayCommand(
                async _ => await LoadOrdersAsync());

            SearchCommand = new AsyncRelayCommand(
                async _ => await SearchAsync());

            NewOrderCommand = new RelayCommand(_ =>
                _navigation.NavigateTo("NewOrder"));

            ViewOrderCommand = new RelayCommand(
                _ => OpenOrderDetail(),
                _ => SelectedOrder != null);

            AddPaymentCommand = new AsyncRelayCommand(
                async _ => await OpenAddPaymentAsync(),
                _ => SelectedOrder != null &&
                     SelectedOrder.RemainingAmount > 0);

            SavePaymentCommand = new AsyncRelayCommand(
                SavePaymentAsync,
                _ => CanSavePayment());

            CancelPaymentCommand = new RelayCommand(
                _ =>
                {
                    IsPaymentFormVisible = false;
                    ClearMessages();
                    CommandManager.InvalidateRequerySuggested();
                });

            PrintInvoiceCommand = new AsyncRelayCommand(
                async _ => await PrintInvoiceAsync(),
                _ => SelectedOrder != null);

            CancelOrderCommand = new AsyncRelayCommand(
                async _ => await CancelOrderAsync(),
                _ => SelectedOrder != null &&
                     SelectedOrder.OrderStatus != "ملغي" &&
                     CurrentSession.Instance.HasPermission("DeleteOrder"));
        }

        private bool CanSavePayment()
        {
            if (SelectedOrder == null || !IsPaymentFormVisible)
                return false;
            if (IsLoading)
                return false;
            if (PayAmount <= 0 ||
                PayAmount > SelectedOrder.RemainingAmount)
                return false;
            if (PayAsElectronic &&
                string.IsNullOrWhiteSpace(PayWallet))
                return false;
            return true;
        }

        // ─── Load ────────────────────────────────────
        public async Task LoadOrdersAsync()
        {
            try
            {
                IsLoading = true;
                var orders = await _orderService
                    .GetOrdersByDateAsync(FromDate, ToDate);

                _allOrders = new ObservableCollection<OrderListDto>(orders);
                FilterOrders();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل الطلبات: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadOrdersAsync();
                return;
            }

            try
            {
                IsLoading = true;
                var results = await _orderService
                    .SearchOrdersAsync(SearchText);

                _allOrders = new ObservableCollection<OrderListDto>(results);
                FilterOrders();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في البحث: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ─── Filter ──────────────────────────────────
        private void FilterOrders()
        {
            var filtered = SelectedFilter switch
            {
                "مسدد" => _allOrders.Where(
                    o => o.PaymentStatus == "مسدد"),
                "جزئي" => _allOrders.Where(
                    o => o.PaymentStatus == "جزئي"),
                "غير مسدد" => _allOrders.Where(
                    o => o.PaymentStatus == "غير مسدد"),
                "متأخر" => _allOrders.Where(o => o.IsOverdue),
                "رهن" => _allOrders.Where(o => o.HasPledge),
                _ => _allOrders.AsEnumerable()
            };

            Orders.Clear();
            foreach (var o in filtered)
                Orders.Add(o);

            UpdateTotals();
        }

        private void UpdateTotals()
        {
            OrdersCount = Orders.Count;
            TotalAmount = Orders.Sum(o => o.TotalAmount);
            TotalPaid = Orders.Sum(o => o.PaidAmount);
            TotalRemaining = Orders.Sum(o => o.RemainingAmount);
        }

        private void OpenOrderDetail()
        {
            if (SelectedOrder == null) return;
            // يمكن لاحقاً: نافذة تفاصيل كاملة
        }

        private Task OpenAddPaymentAsync()
        {
            if (SelectedOrder == null ||
                SelectedOrder.RemainingAmount <= 0)
                return Task.CompletedTask;

            ClearMessages();
            PayAmount = SelectedOrder.RemainingAmount;
            PayAsElectronic = true;
            PayWallet = WalletOptions[0];
            PayReference = null;
            PayNotes = null;
            IsPaymentFormVisible = true;
            CommandManager.InvalidateRequerySuggested();
            return Task.CompletedTask;
        }

        private async Task SavePaymentAsync(object? _)
        {
            if (!CanSavePayment() || SelectedOrder == null)
                return;

            try
            {
                IsLoading = true;
                ClearMessages();
                CommandManager.InvalidateRequerySuggested();

                var dto = new AddPaymentDto
                {
                    OrderId = SelectedOrder.Id,
                    Amount = PayAmount,
                    Method = PayAsElectronic
                        ? PaymentMethod.Electronic
                        : PaymentMethod.Cash,
                    WalletName = PayAsElectronic
                        ? PayWallet?.Trim()
                        : null,
                    Reference = string.IsNullOrWhiteSpace(PayReference)
                        ? null
                        : PayReference.Trim(),
                    Notes = string.IsNullOrWhiteSpace(PayNotes)
                        ? null
                        : PayNotes.Trim()
                };

                var result = await _orderService.AddPaymentAsync(dto);

                if (result.Success)
                {
                    ShowSuccess(result.Message);
                    IsPaymentFormVisible = false;
                    await LoadOrdersAsync();
                }
                else
                    ShowError(result.Message);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task PrintInvoiceAsync()
        {
            if (SelectedOrder == null) return;

            try
            {
                IsLoading = true;
                ClearMessages();
                await _reportService.PrintInvoiceAsync(SelectedOrder.Id);
                ShowSuccess("تم إرسال الفاتورة للطباعة");
            }
            catch (Exception ex)
            {
                ShowError($"تعذر طباعة الفاتورة: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CancelOrderAsync()
        {
            if (SelectedOrder == null) return;

            var result = System.Windows.MessageBox.Show(
                $"هل تريد إلغاء الطلب {SelectedOrder.OrderNumber}؟",
                "تأكيد الإلغاء",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning,
                System.Windows.MessageBoxResult.No,
                System.Windows.MessageBoxOptions.RightAlign |
                System.Windows.MessageBoxOptions.RtlReading);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            var cancelResult = await _orderService
                .CancelOrderAsync(SelectedOrder.Id, "إلغاء يدوي");

            if (cancelResult.Success)
            {
                ShowSuccess(cancelResult.Message);
                await LoadOrdersAsync();
            }
            else
            {
                ShowError(cancelResult.Message);
            }
        }
    }
}
