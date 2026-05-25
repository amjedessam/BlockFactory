using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Customers;
using BlockFactory.Core.Models.Sales;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using BlockFactory.Core.DTOs.Customers;
using BlockFactory.Desktop.Mappers;

namespace BlockFactory.Desktop.ViewModels.Orders
{
    public class NewOrderViewModel : BaseViewModel
    {
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;

        // تفويض عرض dialog الطباعة للـ View (MVVM-safe)
        public Func<Task<bool>>? PrintRequested { get; set; }

        public NewOrderViewModel(
            IOrderService orderService,
            ICustomerService customerService,
            IProductService productService)
        {
            _orderService = orderService;
            _customerService = customerService;
            _productService = productService;
            InitializeCommands();
            InitializeDefaults();
        }

        // ─── بيانات العميل ──────────────────────────

        private CancellationTokenSource? _searchCts;

        private string _customerSearch = string.Empty;
        /* public string CustomerSearch
         {
             get => _customerSearch;
             set
             {
                 SetProperty(ref _customerSearch, value);
                 _ = SearchCustomersDebounced(value);
             }
         }*/

        private ObservableCollection<CustomerLookupDto> _customerResults = new();
        public ObservableCollection<CustomerLookupDto> CustomerResults
        {
            get => _customerResults;
            set => SetProperty(ref _customerResults, value);
        }


        private CustomerLookupDto? _selectedCustomer;
        // ✅ إضافة flag لمنع البحث عند اختيار العميل
        private bool _isSelectingCustomer = false;
        // ✅ تنظيف الـ setter بالكامل
        // ✅ setter نظيف — لا خوف من null خارجي
        public CustomerLookupDto? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                SetProperty(ref _selectedCustomer, value);

                if (value != null)
                {
                    _isSelectingCustomer = true;
                    CustomerSearch = value.FullName;
                    CustomerResults.Clear();
                    _isSelectingCustomer = false;
                }

                // يعمل دائماً — اختيار أو مسح
                OnPropertyChanged(nameof(HasSelectedCustomer));
                OnPropertyChanged(nameof(CustomerDebtText));
                CommandManager.InvalidateRequerySuggested();
            }
        }
        public string CustomerSearch
        {
            get => _customerSearch;
            set
            {
                SetProperty(ref _customerSearch, value);
                if (!_isSelectingCustomer)         // ← لا تبحث عند الاختيار
                    _ = SearchCustomersDebounced(value);
            }
        }
        /*     public CustomerLookupDto? SelectedCustomer
             {
                 get => _selectedCustomer;
                 set
                 {
                     SetProperty(ref _selectedCustomer, value);
                     if (value != null)
                     {
                         CustomerSearch = value.FullName;
                         CustomerResults.Clear();
                         OnPropertyChanged(nameof(HasSelectedCustomer));
                         OnPropertyChanged(nameof(CustomerDebtText));
                     }
                     CommandManager.InvalidateRequerySuggested();
                 }
             }
        */
        public bool HasSelectedCustomer => SelectedCustomer != null;

        public string CustomerDebtText => SelectedCustomer == null
            ? string.Empty
            : SelectedCustomer.TotalDebt > 0
                ? $"⚠️ دين سابق: {SelectedCustomer.TotalDebt:N0} ر.ي"
                : "✅ لا يوجد دين سابق";

        // ─── إضافة عميل سريعة ───────────────────────

        private bool _isQuickAddVisible;
        public bool IsQuickAddVisible
        {
            get => _isQuickAddVisible;
            set => SetProperty(ref _isQuickAddVisible, value);
        }

        private string _quickAddName = string.Empty;
        public string QuickAddName
        {
            get => _quickAddName;
            set => SetProperty(ref _quickAddName, value);
        }

        private string _quickAddPhone = string.Empty;
        public string QuickAddPhone
        {
            get => _quickAddPhone;
            set => SetProperty(ref _quickAddPhone, value);
        }

        private string _quickAddAddress = string.Empty;
        public string QuickAddAddress
        {
            get => _quickAddAddress;
            set => SetProperty(ref _quickAddAddress, value);
        }

        private string _quickAddError = string.Empty;
        public string QuickAddError
        {
            get => _quickAddError;
            set
            {
                SetProperty(ref _quickAddError, value);
                OnPropertyChanged(nameof(HasQuickAddError));
            }
        }

        public bool HasQuickAddError => !string.IsNullOrEmpty(_quickAddError);

        public RelayCommand ShowQuickAddCommand { get; private set; } = null!;
        public RelayCommand CancelQuickAddCommand { get; private set; } = null!;
        public AsyncRelayCommand SaveQuickAddCommand { get; private set; } = null!;

        // ─── بيانات الطلب ───────────────────────────

        private DateTime _orderDate = DateTime.Today;
        public DateTime OrderDate
        {
            get => _orderDate;
            set => SetProperty(ref _orderDate, value);
        }

        private PaymentType _selectedPaymentType = PaymentType.Cash;
        public PaymentType SelectedPaymentType
        {
            get => _selectedPaymentType;
            set
            {
                SetProperty(ref _selectedPaymentType, value);
                OnPropertyChanged(nameof(IsCredit));
                OnPropertyChanged(nameof(IsPledge));
                OnPropertyChanged(nameof(IsElectronic));
                OnPropertyChanged(nameof(ShowDueDate));
                OnPropertyChanged(nameof(ShowWallet));
                OnPropertyChanged(nameof(ShowInitialPayment));
                RecalculateTotals();
            }
        }

        public bool IsCredit => SelectedPaymentType == PaymentType.Credit;
        public bool IsPledge => SelectedPaymentType == PaymentType.Pledge;
        public bool IsElectronic => SelectedPaymentType == PaymentType.Electronic;
        public bool ShowDueDate => IsCredit || IsPledge;
        public bool ShowWallet => IsElectronic;
        public bool ShowInitialPayment =>
            IsCredit || IsPledge || SelectedPaymentType == PaymentType.Mixed;

        private DateTime? _dueDate;
        public DateTime? DueDate
        {
            get => _dueDate;
            set => SetProperty(ref _dueDate, value);
        }

        private DeliveryType _deliveryType = DeliveryType.Pickup;
        public DeliveryType SelectedDeliveryType
        {
            get => _deliveryType;
            set
            {
                SetProperty(ref _deliveryType, value);
                OnPropertyChanged(nameof(IsDelivery));
                RecalculateTotals();
            }
        }

        public bool IsDelivery => SelectedDeliveryType == DeliveryType.Delivery;

        private decimal _deliveryCost;
        public decimal DeliveryCost
        {
            get => _deliveryCost;
            set
            {
                SetProperty(ref _deliveryCost, value);
                RecalculateTotals();
            }
        }

        private decimal _discount;
        public decimal Discount
        {
            get => _discount;
            set
            {
                SetProperty(ref _discount, value);
                RecalculateTotals();
            }
        }

        private decimal _initialPayment;
        public decimal InitialPayment
        {
            get => _initialPayment;
            set => SetProperty(ref _initialPayment, value);
        }

        private string? _selectedWallet;
        public string? SelectedWallet
        {
            get => _selectedWallet;
            set => SetProperty(ref _selectedWallet, value);
        }

        private string? _transactionReference;
        public string? TransactionReference
        {
            get => _transactionReference;
            set => SetProperty(ref _transactionReference, value);
        }

        private string? _notes;
        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        // ─── بيانات الرهن ───────────────────────────

        private string _pledgeDescription = string.Empty;
        public string PledgeDescription
        {
            get => _pledgeDescription;
            set => SetProperty(ref _pledgeDescription, value);
        }

        private PledgeType _pledgeType = PledgeType.Gold;
        public PledgeType SelectedPledgeType
        {
            get => _pledgeType;
            set => SetProperty(ref _pledgeType, value);
        }

        private DateTime _pledgeDueDate = DateTime.Today.AddDays(30);
        public DateTime PledgeDueDate
        {
            get => _pledgeDueDate;
            set => SetProperty(ref _pledgeDueDate, value);
        }

        private string? _pledgeNotes;
        public string? PledgeNotes
        {
            get => _pledgeNotes;
            set => SetProperty(ref _pledgeNotes, value);
        }

        // ─── عناصر الطلب ────────────────────────────

        public ObservableCollection<OrderItemViewModel> OrderItems { get; } = new();
        public ObservableCollection<ProductLookupDto> AvailableProducts { get; } = new();

        private ProductLookupDto? _selectedProduct;
        public ProductLookupDto? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                SetProperty(ref _selectedProduct, value);
                if (value != null)
                {
                    NewItemQuantity = 1;
                    NewItemPrice = value.DefaultPrice;
                    OnPropertyChanged(nameof(NewItemPriceRange));
                }
            }
        }

        private int _newItemQuantity = 1;
        public int NewItemQuantity
        {
            get => _newItemQuantity;
            set => SetProperty(ref _newItemQuantity, value);
        }

        private decimal _newItemPrice;
        public decimal NewItemPrice
        {
            get => _newItemPrice;
            set
            {
                SetProperty(ref _newItemPrice, value);
                OnPropertyChanged(nameof(IsPriceValid));
            }
        }

        public string NewItemPriceRange => SelectedProduct == null
            ? string.Empty
            : $"النطاق: {SelectedProduct.PriceMin:N0} — {SelectedProduct.PriceMax:N0} ر.ي";

        public bool IsPriceValid => SelectedProduct == null ||
            (NewItemPrice >= SelectedProduct.PriceMin &&
             NewItemPrice <= SelectedProduct.PriceMax);

        // ─── الإجماليات ─────────────────────────────

        private decimal _subTotal;
        public decimal SubTotal
        {
            get => _subTotal;
            set => SetProperty(ref _subTotal, value);
        }

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            // ✅ setter بسيط فقط — InvalidateRequerySuggested تُستدعى
            // في الأماكن الصريحة (AddItem, RemoveItem, ClearForm, SaveAsync)
            // وليس داخل setter لتجنب re-entrancy conflict
            set => SetProperty(ref _totalAmount, value);
        }

        private decimal _remainingAmount;
        public decimal RemainingAmount
        {
            get => _remainingAmount;
            set => SetProperty(ref _remainingAmount, value);
        }

        // ─── قوائم الاختيارات ───────────────────────

        public List<PaymentTypeItem> PaymentTypes { get; } = new()
        {
            new("نقد", PaymentType.Cash),
            new("تحويل", PaymentType.Electronic),
            new("آجل", PaymentType.Credit),
            new("رهن", PaymentType.Pledge),
            new("مختلط", PaymentType.Mixed)
        };

        public List<DeliveryTypeItem> DeliveryTypes { get; } = new()
        {
            new("استلام من المصنع", DeliveryType.Pickup),
            new("توصيل", DeliveryType.Delivery)
        };

        public List<string> WalletOptions { get; } = new()
        {
            "كاش",
            "سبأفون",
            "وان كاش"
        };

        public List<PledgeTypeItem> PledgeTypes { get; } = new()
        {
            new("ذهب", PledgeType.Gold),
            new("سلاح", PledgeType.Weapon),
            new("ورقة أرض", PledgeType.LandDeed),
            new("أخرى", PledgeType.Other)
        };

        // ─── Commands ───────────────────────────────

        public AsyncRelayCommand LoadDataCommand { get; private set; } = null!;
        public RelayCommand AddItemCommand { get; private set; } = null!;
        public RelayCommand RemoveItemCommand { get; private set; } = null!;
        public AsyncRelayCommand SaveOrderCommand { get; private set; } = null!;
        public RelayCommand ClearFormCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            LoadDataCommand = new AsyncRelayCommand(
                async _ => await LoadProductsAsync());

            AddItemCommand = new RelayCommand(
                _ => AddItem(),
                _ => SelectedProduct != null &&
                     NewItemQuantity > 0 &&
                     IsPriceValid);

            RemoveItemCommand = new RelayCommand(param =>
            {
                if (param is OrderItemViewModel item)
                {
                    OrderItems.Remove(item);
                    RecalculateTotals();
                    CommandManager.InvalidateRequerySuggested();
                }
            });

            SaveOrderCommand = new AsyncRelayCommand(
                async _ => await SaveOrderAsync(),
                _ => CanSave());

            ClearFormCommand = new RelayCommand(_ => ClearForm());

            // ─── إضافة عميل سريعة ───────────────────
            ShowQuickAddCommand = new RelayCommand(_ =>
            {
                QuickAddName = CustomerSearch.Trim();
                QuickAddPhone = string.Empty;
                QuickAddAddress = string.Empty;
                QuickAddError = string.Empty;
                CustomerResults.Clear();
                IsQuickAddVisible = true;
            });

            CancelQuickAddCommand = new RelayCommand(_ =>
            {
                IsQuickAddVisible = false;
                QuickAddName = string.Empty;
                QuickAddPhone = string.Empty;
                QuickAddAddress = string.Empty;
                QuickAddError = string.Empty;
            });

            SaveQuickAddCommand = new AsyncRelayCommand(
                async _ =>
                {
                    if (string.IsNullOrWhiteSpace(QuickAddName))
                    {
                        QuickAddError = "اسم العميل مطلوب";
                        return;
                    }
                    try
                    {
                        IsLoading = true;
                        QuickAddError = string.Empty;

                        var result = await _customerService.CreateCustomerAsync(
                            new BlockFactory.Core.DTOs.Customers.CreateCustomerDto
                            {
                                FullName = QuickAddName.Trim(),
                                Phone = QuickAddPhone.Trim(),
                                Address = QuickAddAddress.Trim()
                            });

                        if (result.Success)
                        {
                            // تحميل العميل الجديد وتحديده تلقائياً
                            var customers = await _customerService
                                .SearchCustomersAsync(QuickAddName.Trim());

                            var found = customers
                                .FirstOrDefault(c =>
                                    c.FullName.Equals(
                                        QuickAddName.Trim(),
                                        StringComparison.OrdinalIgnoreCase));

                            if (found != null)
                                SelectedCustomer = new CustomerLookupDto
                                {
                                    Id = found.Id,
                                    FullName = found.FullName,
                                    Phone = found.Phone,
                                    TotalDebt = found.TotalDebt
                                };

                            IsQuickAddVisible = false;
                            QuickAddName = string.Empty;
                            QuickAddPhone = string.Empty;
                            QuickAddAddress = string.Empty;
                        }
                        else
                        {
                            QuickAddError = result.Message;
                        }
                    }
                    catch (Exception ex)
                    {
                        QuickAddError = $"خطأ: {ex.Message}";
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                },
                _ => !string.IsNullOrWhiteSpace(QuickAddName));
        }

        private void InitializeDefaults()
        {
            OrderDate = DateTime.Today;
            PledgeDueDate = DateTime.Today.AddDays(30);
        }

        // ─── Logic ──────────────────────────────────

        private async Task LoadProductsAsync()
        {
            try
            {
                IsLoading = true;
                var products = await _productService.GetActiveProductsAsync();
                AvailableProducts.Clear();
                foreach (var p in products)
                    AvailableProducts.Add(ProductLookupMapper.ToLookupDto(p));
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل المنتجات: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Debounce 300ms لمنع استدعاء API عند كل حرف
        private async Task SearchCustomersDebounced(string keyword)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                    await SearchCustomersAsync(keyword);
            }
            catch (TaskCanceledException)
            {
                // متوقع — المستخدم يكتب بسرعة
            }
        }

        private async Task SearchCustomersAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
            {
                CustomerResults.Clear();
                return;
            }

            var results = await _customerService.SearchCustomersAsync(keyword);

            CustomerResults.Clear();
            foreach (var c in results)
                CustomerResults.Add(new CustomerLookupDto
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Phone = c.Phone,
                    TotalDebt = c.TotalDebt
                });
        }

        private void AddItem()
        {
            if (SelectedProduct == null) return;

            var existing = OrderItems.FirstOrDefault(
                i => i.ProductId == SelectedProduct.Id);

            if (existing != null)
            {
                existing.Quantity += NewItemQuantity;
                existing.RecalculateTotal();
            }
            else
            {
                var item = new OrderItemViewModel
                {
                    ProductId = SelectedProduct.Id,
                    ProductName = SelectedProduct.Name,
                    PriceMin = SelectedProduct.PriceMin,
                    PriceMax = SelectedProduct.PriceMax,
                    UnitPrice = NewItemPrice,
                    Quantity = NewItemQuantity,
                    AvailableStock = SelectedProduct.AvailableStock
                };
                item.RecalculateTotal();
                item.OnTotalChanged += RecalculateTotals;
                OrderItems.Add(item);
            }

            SelectedProduct = null;
            NewItemQuantity = 1;
            NewItemPrice = 0;
            RecalculateTotals();
            CommandManager.InvalidateRequerySuggested();
        }

        private void RecalculateTotals()
        {
            SubTotal = OrderItems.Sum(i => i.TotalPrice);
            var delivery = IsDelivery ? DeliveryCost : 0;
            TotalAmount = SubTotal - Discount + delivery;

            RemainingAmount = SelectedPaymentType == PaymentType.Cash
                ? 0
                : TotalAmount - InitialPayment;

            if (RemainingAmount < 0) RemainingAmount = 0;
        }

        private bool CanSave()
        {
            return HasSelectedCustomer &&
                   OrderItems.Count > 0 &&
                   TotalAmount > 0 &&
                   !IsLoading;
        }

        private async Task SaveOrderAsync()
        {
            if (!CanSave()) return;

            if (IsPledge && string.IsNullOrWhiteSpace(PledgeDescription))
            {
                ShowError("يرجى إدخال وصف الرهن");
                return;
            }

            if (ShowDueDate && DueDate == null)
            {
                ShowError("يرجى تحديد تاريخ الاستحقاق");
                return;
            }

            try
            {
                IsLoading = true;
                ClearMessages();

                var dto = new CreateOrderDto
                {
                    CustomerId = SelectedCustomer!.Id,
                    OrderDate = OrderDate,
                    DueDate = DueDate,
                    PaymentType = SelectedPaymentType,
                    DeliveryType = SelectedDeliveryType,
                    DeliveryCost = IsDelivery ? DeliveryCost : 0,
                    Discount = Discount,

                    // ✅ الإصلاح الجوهري:
                    // نقد فقط = مدفوع بالكامل
                    // أي نوع آخر = الدفعة الأولى التي أدخلها المستخدم
                    InitialPayment = SelectedPaymentType == PaymentType.Cash
                        ? TotalAmount
                        : InitialPayment,

                    ElectronicWalletName = SelectedWallet,
                    TransactionReference = TransactionReference,
                    Notes = Notes,

                    Items = OrderItems.Select(i => new CreateOrderItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        PriceMin = i.PriceMin,
                        PriceMax = i.PriceMax
                    }).ToList(),

                    Pledge = IsPledge ? new CreatePledgeDto
                    {
                        Description = PledgeDescription,
                        PledgeType = SelectedPledgeType,
                        DueDate = PledgeDueDate,
                        Notes = PledgeNotes
                    } : null
                };

                var result = await _orderService.CreateOrderAsync(dto);

                if (result.Success)
                {
                    ShowSuccess(result.Message);

                    bool shouldPrint = PrintRequested != null &&
                                       await PrintRequested.Invoke();

                    if (shouldPrint)
                    {
                        try
                        {
                            // ① جلب بايتات الـ PDF
                            var pdfBytes = await _orderService
                                .GenerateInvoicePdfAsync(result.Data);

                            if (pdfBytes != null && pdfBytes.Length > 0)
                            {
                                // ② حفظ الملف في مجلد المستندات
                                var folder = System.IO.Path.Combine(
                                    Environment.GetFolderPath(
                                        Environment.SpecialFolder.MyDocuments),
                                    "BlockFactory_Invoices");

                                System.IO.Directory.CreateDirectory(folder);

                                var fileName = $"Invoice_{result.Data}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                                var filePath = System.IO.Path.Combine(folder, fileName);

                                await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                                // ③ فتح الـ PDF بالتطبيق الافتراضي
                                System.Diagnostics.Process.Start(
                                    new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = filePath,
                                        UseShellExecute = true
                                    });
                            }
                            else
                            {
                                ShowError("⚠️ لم يتم توليد الفاتورة — تحقق من بيانات الطلب");
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowError($"خطأ في الطباعة: {ex.Message}");
                        }
                    }

                    ClearForm();
                }
                else
                {
                    ShowError(result.Message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ غير متوقع: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ClearForm()
        {
            SelectedCustomer = null;
            CustomerSearch = string.Empty;
            OrderItems.Clear();
            SelectedPaymentType = PaymentType.Cash;
            SelectedDeliveryType = DeliveryType.Pickup;
            DeliveryCost = 0;
            Discount = 0;
            InitialPayment = 0;
            DueDate = null;
            SelectedWallet = null;
            TransactionReference = null;
            Notes = null;
            PledgeDescription = string.Empty;
            PledgeDueDate = DateTime.Today.AddDays(30);
            PledgeNotes = null;
            SubTotal = 0;
            TotalAmount = 0;
            RemainingAmount = 0;
            ClearMessages();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // ─── Helper Classes ──────────────────────────────

    public class OrderItemViewModel : BaseViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
        public int AvailableStock { get; set; }

        public event Action? OnTotalChanged;

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                SetProperty(ref _quantity, value);
                RecalculateTotal();
                OnPropertyChanged(nameof(IsLowStock));
            }
        }

        private decimal _unitPrice;
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                SetProperty(ref _unitPrice, value);
                RecalculateTotal();
            }
        }

        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set => SetProperty(ref _totalPrice, value);
        }

        public string PriceRangeText => $"{PriceMin:N0} — {PriceMax:N0}";
        public bool IsLowStock => AvailableStock < Quantity;

        public void RecalculateTotal()
        {
            TotalPrice = Quantity * UnitPrice;
            OnTotalChanged?.Invoke();
        }
    }

    public record PaymentTypeItem(string Name, PaymentType Value);
    public record DeliveryTypeItem(string Name, DeliveryType Value);
    public record PledgeTypeItem(string Name, PledgeType Value);

    public class ProductLookupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
        public decimal DefaultPrice { get; set; }
        public int AvailableStock { get; set; }
    }
}