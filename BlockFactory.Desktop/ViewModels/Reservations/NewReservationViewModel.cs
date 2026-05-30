// BlockFactory.Desktop/ViewModels/Reservations/NewReservationViewModel.cs

using BlockFactory.Core.DTOs.Customers;
using BlockFactory.Core.DTOs.Reservations;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Reservations;
using BlockFactory.Core.Models.Sales;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;

namespace BlockFactory.Desktop.ViewModels.Reservations
{
    public class NewReservationViewModel : BaseViewModel
    {
        private readonly IReservationService _reservationService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;

        // ─── تفويض dialog التأكيد للـ View (MVVM-safe) ─
        // تُسنَد من NewReservationView.xaml.cs مثلما هو في واجهة المبيعات
        public Func<string, Task<bool>>? ConfirmRequested { get; set; }

        public NewReservationViewModel(
            IReservationService reservationService,
            ICustomerService customerService,
            IProductService productService)
        {
            _reservationService = reservationService;
            _customerService = customerService;
            _productService = productService;
            InitializeCommands();
            ReservationDate = DateTime.Today;
        }

        // ─── بيانات العميل ──────────────────────────

        private string _customerSearch = string.Empty;
        private bool _isSelectingCustomer = false;

        public string CustomerSearch
        {
            get => _customerSearch;
            set
            {
                if (_selectedCustomer != null &&
                    value != _selectedCustomer.FullName)
                {
                    _selectedCustomer = null;
                    OnPropertyChanged(nameof(SelectedCustomer));
                    OnPropertyChanged(nameof(HasCustomer));
                }
                SetProperty(ref _customerSearch, value);
                if (!_isSelectingCustomer)
                    _ = SearchCustomersAsync(value);
            }
        }

        public ObservableCollection<CustomerLookupDto> CustomerResults { get; }
            = new();

        private CustomerLookupDto? _selectedCustomer;
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
                    _isSelectingCustomer = false;
                    OnPropertyChanged(nameof(HasCustomer));
                }
            }
        }

        public bool HasCustomer => SelectedCustomer != null;

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

        // ─── نوع الحجز ──────────────────────────────

        private ReservationType _reservationType = ReservationType.QuantityReservation;
        public ReservationType ReservationType
        {
            get => _reservationType;
            set
            {
                SetProperty(ref _reservationType, value);
                OnPropertyChanged(nameof(IsQuantityReservation));
                OnPropertyChanged(nameof(IsOpenBalance));
                OnPropertyChanged(nameof(ReservationTypeLabel));
                // مسح الأصناف عند تغيير النوع
                if (value == ReservationType.OpenBalance)
                    ClearAllItems();
            }
        }

        public bool IsQuantityReservation
            => ReservationType == ReservationType.QuantityReservation;
        public bool IsOpenBalance
            => ReservationType == ReservationType.OpenBalance;

        public string ReservationTypeLabel => IsQuantityReservation
            ? "حجز محدد — تحديد الكميات والأنواع"
            : "حجز مفتوح — رصيد مالي بدون تحديد";

        // ─── بيانات الدفع ───────────────────────────

        private decimal _amountPaid;
        public decimal AmountPaid
        {
            get => _amountPaid;
            set
            {
                SetProperty(ref _amountPaid, value);
                OnPropertyChanged(nameof(IsAmountValid));
                // تحديث تنبيه الفرق فور تغيير المبلغ
                OnPropertyChanged(nameof(HasAmountMismatch));
                OnPropertyChanged(nameof(AmountMismatchHint));
            }
        }

        public bool IsAmountValid => AmountPaid > 0;

        private PaymentMethod _paymentMethod = PaymentMethod.Cash;
        public PaymentMethod SelectedPaymentMethod
        {
            get => _paymentMethod;
            set
            {
                SetProperty(ref _paymentMethod, value);
                OnPropertyChanged(nameof(ShowWallet));
            }
        }

        public bool ShowWallet =>
            SelectedPaymentMethod == PaymentMethod.Electronic;

        private string? _walletName;
        public string? WalletName
        {
            get => _walletName;
            set => SetProperty(ref _walletName, value);
        }

        private string? _transactionReference;
        public string? TransactionReference
        {
            get => _transactionReference;
            set => SetProperty(ref _transactionReference, value);
        }

        public List<string> WalletOptions { get; } = new()
        { "كاش", "سبأفون", "وان كاش" };

        public List<PaymentMethodItem> PaymentMethods { get; } = new()
        {
            new("نقد", PaymentMethod.Cash),
            new("تحويل إلكتروني", PaymentMethod.Electronic)
        };

        // ─── التاريخ والملاحظات ──────────────────────

        private DateTime _reservationDate = DateTime.Today;
        public DateTime ReservationDate
        {
            get => _reservationDate;
            set => SetProperty(ref _reservationDate, value);
        }

        private string? _notes;
        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        // ─── أصناف الحجز المحدد ─────────────────────

        public ObservableCollection<ReservationItemEntry> ReservationItems { get; }
            = new();

        public ObservableCollection<ProductLookupDto> AvailableProducts { get; }
            = new();

        private ProductLookupDto? _selectedProduct;
        public ProductLookupDto? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                SetProperty(ref _selectedProduct, value);
                if (value != null)
                {
                    NewItemPrice = value.DefaultPrice;
                    NewItemQuantity = 0;   // لا تعيين تلقائي — المستخدم يدخل الكمية
                    OnPropertyChanged(nameof(NewItemPriceRange));
                }
            }
        }

        private int _newItemQuantity = 0;
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

        // ─── إجمالي الحجز المحدد ────────────────────

        public decimal ItemsTotal => ReservationItems.Sum(i => i.Total);

        // ─── تنبيه الفرق بين الإجمالي والمبلغ المدفوع ─

        /// <summary>
        /// يظهر تنبيهاً أصفر في الملخص إذا كان المبلغ المدفوع
        /// لا يطابق إجمالي الأصناف قبل الضغط على حفظ.
        /// </summary>
        public bool HasAmountMismatch =>
            IsQuantityReservation &&
            ReservationItems.Any() &&
            AmountPaid > 0 &&
            Math.Abs(ItemsTotal - AmountPaid) > 1;

        public string AmountMismatchHint =>
            HasAmountMismatch
                ? $"⚠ فرق {Math.Abs(ItemsTotal - AmountPaid):N0} ر.ي — " +
                  $"يرجى مطابقة المبلغ المدفوع مع الإجمالي"
                : string.Empty;

        // ─── Commands ───────────────────────────────

        public AsyncRelayCommand LoadProductsCommand { get; private set; } = null!;
        public RelayCommand SelectTypeCommand { get; private set; } = null!;
        public RelayCommand AddItemCommand { get; private set; } = null!;
        public RelayCommand RemoveItemCommand { get; private set; } = null!;
        public AsyncRelayCommand SaveCommand { get; private set; } = null!;
        public RelayCommand ClearCommand { get; private set; } = null!;

        public RelayCommand ShowQuickAddCommand { get; private set; } = null!;
        public RelayCommand CancelQuickAddCommand { get; private set; } = null!;
        public AsyncRelayCommand SaveQuickAddCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            LoadProductsCommand = new AsyncRelayCommand(
                async _ => await LoadProductsAsync());

            AddItemCommand = new RelayCommand(
                _ => AddItem(),
                _ => SelectedProduct != null &&
                     NewItemQuantity > 0 && IsPriceValid);

            RemoveItemCommand = new RelayCommand(param =>
            {
                if (param is ReservationItemEntry item)
                {
                    // ✅ إلغاء الاشتراك أولاً لمنع memory leak
                    item.PropertyChanged -= OnItemPropertyChanged;
                    ReservationItems.Remove(item);
                    NotifyTotalsChanged();
                }
            });

            SaveCommand = new AsyncRelayCommand(
                async _ => await SaveAsync());

            SelectTypeCommand = new RelayCommand(param =>
            {
                if (param is string p)
                {
                    ReservationType = p == "Open"
                        ? ReservationType.OpenBalance
                        : ReservationType.QuantityReservation;
                }
            });

            ClearCommand = new RelayCommand(_ => ClearForm());

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
                            new CreateCustomerDto
                            {
                                FullName = QuickAddName.Trim(),
                                Phone = QuickAddPhone.Trim(),
                                Address = QuickAddAddress.Trim()
                            });

                        if (result.Success)
                        {
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

        // ─── Handler مشترك لتغييرات الأصناف ─────────

        /// <summary>
        /// يُستدعى تلقائياً عند تغيير الكمية أو السعر لأي صنف في الجدول.
        /// يحدّث الإجمالي وتنبيه الفرق في الملخص فوراً.
        /// </summary>
        private void OnItemPropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReservationItemEntry.Total))
                NotifyTotalsChanged();
        }

        /// <summary>
        /// يُشعر الـ UI بتحديث جميع الخصائص المرتبطة بالإجمالي.
        /// </summary>
        private void NotifyTotalsChanged()
        {
            OnPropertyChanged(nameof(ItemsTotal));
            OnPropertyChanged(nameof(HasAmountMismatch));
            OnPropertyChanged(nameof(AmountMismatchHint));
        }

        /// <summary>
        /// يحذف جميع الأصناف مع إلغاء الاشتراك لكل منها.
        /// </summary>
        private void ClearAllItems()
        {
            foreach (var item in ReservationItems)
                item.PropertyChanged -= OnItemPropertyChanged;
            ReservationItems.Clear();
            NotifyTotalsChanged();
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
                    AvailableProducts.Add(new ProductLookupDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        DefaultPrice = p.DefaultPrice,
                        PriceMin = p.PriceMin,
                        PriceMax = p.PriceMax,
                        AvailableStock = p.Stock?.FreeQuantity ?? 0
                    });
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

        private async Task SearchCustomersAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
            {
                CustomerResults.Clear();
                return;
            }
            if (_selectedCustomer != null && keyword == _selectedCustomer.FullName)
                return;

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

            var existing = ReservationItems
                .FirstOrDefault(i => i.ProductId == SelectedProduct.Id);

            if (existing != null)
            {
                // ✅ تعديل الكمية على الصنف الموجود مباشرة
                existing.Quantity += NewItemQuantity;
                // OnItemPropertyChanged سيُطلق NotifyTotalsChanged تلقائياً
                return;
            }

            var newItem = new ReservationItemEntry
            {
                ProductId = SelectedProduct.Id,
                ProductName = SelectedProduct.Name,
                PriceMin = SelectedProduct.PriceMin,
                PriceMax = SelectedProduct.PriceMax,
                Quantity = NewItemQuantity,
                UnitPrice = NewItemPrice,
                // ✅ نحفظ المخزون المتاح وقت الإضافة لفحص IsLowStock
                AvailableStock = SelectedProduct.AvailableStock
            };

            // ✅ الاشتراك في حدث التغيير ليتحدث الإجمالي عند تعديل الجدول
            newItem.PropertyChanged += OnItemPropertyChanged;

            ReservationItems.Add(newItem);

            SelectedProduct = null;
            NewItemQuantity = 0;   // إعادة تصفير — المستخدم يدخل الكمية في كل مرة
            NewItemPrice = 0;
            NotifyTotalsChanged();
        }

        private async Task SaveAsync()
        {
            // ── التحقق ──
            if (SelectedCustomer == null)
            {
                ShowError("يرجى اختيار العميل"); return;
            }
            if (AmountPaid <= 0)
            {
                ShowError("يرجى إدخال المبلغ المدفوع"); return;
            }
            if (IsQuantityReservation && !ReservationItems.Any())
            {
                ShowError("الحجز المحدد يتطلب إضافة صنف واحد على الأقل"); return;
            }
            // ─── 1. تحقق المخزون أولاً — تحذير + تأكيد ──────────────────
            // يجب أن يكون قبل فحص تطابق المبلغ حتى لا يُعيق الحفظ بعد الموافقة
            bool skipStockValidation = false;
            if (IsQuantityReservation)
            {
                var lowStockItems = ReservationItems
                    .Where(i => i.IsLowStock)
                    .ToList();

                if (lowStockItems.Any())
                {
                    var itemNames = string.Join(
                        "، ",
                        lowStockItems.Select(i =>
                            $"{i.ProductName} (متوفر: {i.AvailableStock}، مطلوب: {i.Quantity})"));

                    var msg =
                        $"الكمية المطلوبة لبعض المنتجات أكبر من المتوفر في المخزون:\n" +
                        $"{itemNames}\n\n" +
                        $"هل تريد المتابعة وحفظ الحجز رغم ذلك؟";

                    if (ConfirmRequested != null)
                    {
                        bool allowSave = await ConfirmRequested(msg);
                        if (!allowSave) return;   // المستخدم ضغط لا → توقف
                        // المستخدم وافق → نضع علم التجاوز
                        skipStockValidation = true;
                    }
                    // إذا لم يُسنَد ConfirmRequested نكمل الحفظ بدون تجاوز (الخدمة ستحقق)
                }
            }

            // ─── 2. تحقق تطابق المبلغ مع الإجمالي ────────────────────────
            // يأتي بعد موافقة المخزون حتى لا يحجب الحفظ المقصود
            if (IsQuantityReservation)
            {
                var expectedTotal = ReservationItems.Sum(i => i.Total);
                if (Math.Abs(expectedTotal - AmountPaid) > 1)
                {
                    var diff = Math.Abs(expectedTotal - AmountPaid);
                    var direction = expectedTotal > AmountPaid ? "ناقص" : "زائد";
                    ShowError(
                        $"المبلغ المدفوع ({AmountPaid:N0} ر.ي) لا يطابق " +
                        $"إجمالي الأصناف ({expectedTotal:N0} ر.ي).\n" +
                        $"الفرق: {diff:N0} ر.ي ({direction}).\n" +
                        $"يرجى تعديل المبلغ المدفوع أو تعديل الكميات/الأسعار في الجدول.");
                    return;
                }
            }

            try
            {
                IsLoading = true;
                ClearMessages();

                var dto = new CreateReservationDto
                {
                    CustomerId = SelectedCustomer.Id,
                    Type = ReservationType,
                    AmountPaid = AmountPaid,
                    PaymentMethod = SelectedPaymentMethod,
                    WalletName = WalletName,
                    TransactionReference = TransactionReference,
                    ReservationDate = ReservationDate,
                    Notes = Notes,
                    Items = ReservationItems.Select(i =>
                        new CreateReservationItemDto
                        {
                            ProductId = i.ProductId,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice
                        }).ToList()
                    ,
                    SkipStockValidation = skipStockValidation
                };

                var result = await _reservationService.CreateReservationAsync(dto);

                if (result.Success)
                {
                    ShowSuccess(result.Message);
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
            }
        }

        private void ClearForm()
        {
            SelectedCustomer = null;
            CustomerSearch = string.Empty;
            CustomerResults.Clear();
            AmountPaid = 0;
            SelectedPaymentMethod = PaymentMethod.Cash;
            WalletName = null;
            TransactionReference = null;
            ReservationDate = DateTime.Today;
            Notes = null;
            // ✅ استخدام ClearAllItems بدل .Clear() لإلغاء الاشتراكات
            ClearAllItems();
            SelectedProduct = null;
            ReservationType = ReservationType.QuantityReservation;
            ClearMessages();
        }
    }

    // ─── Helper Classes ──────────────────────────────

    public class ReservationItemEntry : BaseViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
        /// <summary>الكمية المتوفرة في المخزون وقت إضافة الصنف</summary>
        public int AvailableStock { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                SetProperty(ref _quantity, value);
                OnPropertyChanged(nameof(Total));
                // يُحدَّث IsLowStock عند تعديل الكمية في الجدول
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
                OnPropertyChanged(nameof(Total));
            }
        }

        public decimal Total => Quantity * UnitPrice;
        public string PriceRangeText => $"{PriceMin:N0} — {PriceMax:N0}";
        /// <summary>
        /// صحيح إذا طلب المستخدم أكثر مما هو متوفر في المخزون.
        /// يُستخدم لإطلاق رسالة التحذير قبل الحفظ.
        /// </summary>
        public bool IsLowStock => AvailableStock < Quantity;
    }

    public class ProductLookupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DefaultPrice { get; set; }
        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
        public int AvailableStock { get; set; }
    }

    public record PaymentMethodItem(string Name, PaymentMethod Value);
}