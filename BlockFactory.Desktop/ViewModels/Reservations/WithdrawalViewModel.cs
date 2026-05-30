// BlockFactory.Desktop/ViewModels/Reservations/WithdrawalViewModel.cs

using BlockFactory.Core.DTOs.Reservations;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;

namespace BlockFactory.Desktop.ViewModels.Reservations
{
    public class WithdrawalViewModel : BaseViewModel
    {
        private readonly IReservationService _reservationService;

        public WithdrawalViewModel(IReservationService reservationService)
        {
            _reservationService = reservationService;
            InitializeCommands();
        }

        // ─── بيانات الحجز المختار ───────────────────

        private int _reservationId;
        public int ReservationId
        {
            get => _reservationId;
            set
            {
                SetProperty(ref _reservationId, value);
                _ = LoadReservationAsync(value);
            }
        }

        private ReservationDetailDto? _reservation;
        public ReservationDetailDto? Reservation
        {
            get => _reservation;
            set
            {
                SetProperty(ref _reservation, value);
                OnPropertyChanged(nameof(IsQuantityReservation));
                OnPropertyChanged(nameof(IsOpenBalance));
                OnPropertyChanged(nameof(HasReservation));
                LoadProductsFromSnapshot();
            }
        }

        public bool HasReservation      => Reservation != null;
        public bool IsQuantityReservation =>
            Reservation?.Type == Core.Models.Reservations.ReservationType.QuantityReservation;
        public bool IsOpenBalance =>
            Reservation?.Type == Core.Models.Reservations.ReservationType.OpenBalance;

        // ─── المنتجات المتاحة للسحب ─────────────────
        // مأخوذة من PriceSnapshot — بالسعر المثبت

        public ObservableCollection<WithdrawalProductEntry> AvailableProducts { get; }
            = new();

        private WithdrawalProductEntry? _selectedProduct;
        public WithdrawalProductEntry? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                SetProperty(ref _selectedProduct, value);
                if (value != null)
                {
                    NewItemQuantity = 1;
                    OnPropertyChanged(nameof(AvailableQuantityText));
                    OnPropertyChanged(nameof(FixedPrice));
                }
            }
        }

        /// <summary>السعر المثبت من الـ Snapshot — لا يمكن تغييره</summary>
        public decimal FixedPrice => SelectedProduct?.FixedPrice ?? 0;

        public string AvailableQuantityText
        {
            get
            {
                if (SelectedProduct == null) return string.Empty;
                if (IsQuantityReservation)
                    return $"المتبقي المحجوز: {SelectedProduct.QuantityRemaining:N0}";
                return $"الرصيد المتبقي: {Reservation?.AmountRemaining:N0} ر.ي";
            }
        }

        private int _newItemQuantity = 1;
        public int NewItemQuantity
        {
            get => _newItemQuantity;
            set => SetProperty(ref _newItemQuantity, value);
        }

        // ─── أصناف السحب ────────────────────────────

        public ObservableCollection<WithdrawalItemEntry> WithdrawalItems { get; }
            = new();

        public decimal WithdrawalTotal => WithdrawalItems.Sum(i => i.Total);

        // ─── بيانات السحب ───────────────────────────

        private DateTime _withdrawalDate = DateTime.Today;
        public DateTime WithdrawalDate
        {
            get => _withdrawalDate;
            set => SetProperty(ref _withdrawalDate, value);
        }

        private string? _notes;
        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        // ─── Commands ───────────────────────────────

        public RelayCommand      AddItemCommand    { get; private set; } = null!;
        public RelayCommand      RemoveItemCommand { get; private set; } = null!;
        public AsyncRelayCommand SaveCommand       { get; private set; } = null!;
        public RelayCommand      ClearCommand      { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddItemCommand = new RelayCommand(
                _ => AddItem(),
                _ => SelectedProduct != null && NewItemQuantity > 0);

            RemoveItemCommand = new RelayCommand(param =>
            {
                if (param is WithdrawalItemEntry item)
                {
                    WithdrawalItems.Remove(item);
                    OnPropertyChanged(nameof(WithdrawalTotal));
                }
            });

            SaveCommand = new AsyncRelayCommand(
                async _ => await SaveWithdrawalAsync());

            ClearCommand = new RelayCommand(_ => ClearForm());
        }

        // ─── Logic ──────────────────────────────────

        private async Task LoadReservationAsync(int reservationId)
        {
            if (reservationId <= 0) return;
            try
            {
                IsLoading = true;
                Reservation = await _reservationService
                    .GetReservationByIdAsync(reservationId);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل الحجز: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadProductsFromSnapshot()
        {
            AvailableProducts.Clear();
            if (Reservation == null) return;

            foreach (var snap in Reservation.PriceSnapshots)
            {
                // للحجز المحدد: فقط الأصناف الموجودة في الحجز
                if (IsQuantityReservation)
                {
                    var reservedItem = Reservation.Items
                        .FirstOrDefault(i => i.ProductId == snap.ProductId);

                    if (reservedItem == null || reservedItem.QuantityRemaining <= 0)
                        continue;

                    AvailableProducts.Add(new WithdrawalProductEntry
                    {
                        ProductId         = snap.ProductId,
                        ProductName       = snap.ProductName,
                        FixedPrice        = snap.Price,
                        QuantityRemaining = reservedItem.QuantityRemaining
                    });
                }
                else
                {
                    // للحجز المفتوح: كل المنتجات من الـ Snapshot
                    AvailableProducts.Add(new WithdrawalProductEntry
                    {
                        ProductId         = snap.ProductId,
                        ProductName       = snap.ProductName,
                        FixedPrice        = snap.Price,
                        QuantityRemaining = int.MaxValue // لا يوجد حد للكمية
                    });
                }
            }
        }

        private void AddItem()
        {
            if (SelectedProduct == null) return;

            // للحجز المحدد: تحقق من الكمية
            if (IsQuantityReservation &&
                NewItemQuantity > SelectedProduct.QuantityRemaining)
            {
                ShowError(
                    $"الكمية المطلوبة ({NewItemQuantity:N0}) تتجاوز " +
                    $"المتبقي ({SelectedProduct.QuantityRemaining:N0}) " +
                    $"للمنتج: {SelectedProduct.ProductName}");
                return;
            }

            // للحجز المفتوح: تحقق من الرصيد
            if (IsOpenBalance)
            {
                var itemTotal = NewItemQuantity * SelectedProduct.FixedPrice;
                var currentTotal = WithdrawalTotal;
                var remaining = Reservation?.AmountRemaining ?? 0;

                if (currentTotal + itemTotal > remaining)
                {
                    ShowError(
                        $"إجمالي السحب سيتجاوز الرصيد المتبقي " +
                        $"({remaining:N0} ر.ي)");
                    return;
                }
            }

            var existing = WithdrawalItems
                .FirstOrDefault(i => i.ProductId == SelectedProduct.ProductId);

            if (existing != null)
            {
                existing.Quantity += NewItemQuantity;
                OnPropertyChanged(nameof(WithdrawalTotal));
                return;
            }

            WithdrawalItems.Add(new WithdrawalItemEntry
            {
                ProductId   = SelectedProduct.ProductId,
                ProductName = SelectedProduct.ProductName,
                FixedPrice  = SelectedProduct.FixedPrice,
                Quantity    = NewItemQuantity
            });

            SelectedProduct = null;
            NewItemQuantity = 1;
            ClearMessages();
            OnPropertyChanged(nameof(WithdrawalTotal));
        }

        private async Task SaveWithdrawalAsync()
        {
            if (Reservation == null)
            {
                ShowError("لم يتم تحديد الحجز"); return;
            }
            if (!WithdrawalItems.Any())
            {
                ShowError("يرجى إضافة صنف واحد على الأقل"); return;
            }

            try
            {
                IsLoading = true;
                ClearMessages();

                var dto = new CreateWithdrawalDto
                {
                    ReservationId  = Reservation.Id,
                    WithdrawalDate = WithdrawalDate,
                    Notes          = Notes,
                    Items          = WithdrawalItems.Select(i =>
                        new CreateWithdrawalItemDto
                        {
                            ProductId = i.ProductId,
                            Quantity  = i.Quantity
                        }).ToList()
                };

                var result = await _reservationService.CreateWithdrawalAsync(dto);

                if (result.Success)
                {
                    ShowSuccess(result.Message);
                    // إعادة تحميل بيانات الحجز لتحديث الأرصدة
                    await LoadReservationAsync(Reservation.Id);
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
            WithdrawalItems.Clear();
            SelectedProduct = null;
            NewItemQuantity = 1;
            WithdrawalDate  = DateTime.Today;
            Notes           = null;
            ClearMessages();
            OnPropertyChanged(nameof(WithdrawalTotal));
        }
    }

    // ─── Helper Classes ──────────────────────────────

    public class WithdrawalProductEntry
    {
        public int     ProductId         { get; set; }
        public string  ProductName       { get; set; } = string.Empty;
        public decimal FixedPrice        { get; set; }
        public int     QuantityRemaining { get; set; }
        public string  DisplayText =>
            $"{ProductName} — {FixedPrice:N0} ر.ي";
    }

    public class WithdrawalItemEntry : BaseViewModel
    {
        public int    ProductId   { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal FixedPrice { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                SetProperty(ref _quantity, value);
                OnPropertyChanged(nameof(Total));
            }
        }

        public decimal Total => Quantity * FixedPrice;
    }
}
