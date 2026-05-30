// BlockFactory.Desktop/ViewModels/Reservations/ReservationsViewModel.cs

using BlockFactory.Core.DTOs.Reservations;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Reservations;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.Services;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;

namespace BlockFactory.Desktop.ViewModels.Reservations
{
    public class ReservationsViewModel : BaseViewModel
    {
        private readonly IReservationService _reservationService;
        private readonly NavigationService   _navigation;

        public ReservationsViewModel(
            IReservationService reservationService,
            NavigationService navigation)
        {
            _reservationService = reservationService;
            _navigation         = navigation;
            InitializeCommands();
        }

        // ─── البيانات ───────────────────────────────
        public ObservableCollection<ReservationListDto> Reservations { get; }
            = new();

        private ReservationListDto? _selectedReservation;
        public ReservationListDto? SelectedReservation
        {
            get => _selectedReservation;
            set
            {
                SetProperty(ref _selectedReservation, value);
                OnPropertyChanged(nameof(HasSelection));
                _ = LoadDetailAsync();
            }
        }

        private ReservationDetailDto? _detail;
        public ReservationDetailDto? Detail
        {
            get => _detail;
            set => SetProperty(ref _detail, value);
        }

        public bool HasSelection => SelectedReservation != null;

        // ─── البحث والفلتر ──────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                _ = SearchAsync(value);
            }
        }

        private string _selectedFilter = "الكل";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                SetProperty(ref _selectedFilter, value);
                _ = LoadAsync();
            }
        }

        public List<string> FilterOptions { get; } = new()
        {
            "الكل", "حجز محدد", "حجز مفتوح",
            "نشط", "مستهلك جزئياً", "مستهلك كلياً", "ملغي"
        };

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand LoadCommand      { get; private set; } = null!;
        public AsyncRelayCommand NewReservationCommand { get; private set; } = null!;
        public AsyncRelayCommand WithdrawCommand  { get; private set; } = null!;
        public AsyncRelayCommand CancelCommand    { get; private set; } = null!;

        private void InitializeCommands()
        {
            LoadCommand = new AsyncRelayCommand(
                async _ => await LoadAsync());

            NewReservationCommand = new AsyncRelayCommand(
                _ =>
                {
                    _navigation.NavigateTo("NewReservation");
                    return Task.CompletedTask;
                });

            WithdrawCommand = new AsyncRelayCommand(
                _ =>
                {
                    if (SelectedReservation == null) return Task.CompletedTask;
                    _navigation.NavigationParameter = SelectedReservation.Id;
                    _navigation.NavigateTo("Withdrawal");
                    return Task.CompletedTask;
                },
                _ => SelectedReservation != null &&
                     SelectedReservation.AmountRemaining > 0);

            CancelCommand = new AsyncRelayCommand(
                async _ => await CancelReservationAsync(),
                _ => SelectedReservation != null &&
                     (SelectedReservation.StatusText == "نشط" ||
                      SelectedReservation.StatusText == "مستهلك جزئياً"));
        }

        // ─── Logic ──────────────────────────────────

        public async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                var all = await _reservationService.GetAllActiveAsync();

                var filtered = SelectedFilter switch
                {
                    "حجز محدد"       => all.Where(r => r.TypeText == "حجز محدد"),
                    "حجز مفتوح"      => all.Where(r => r.TypeText == "حجز مفتوح"),
                    "نشط"            => all.Where(r => r.StatusText == "نشط"),
                    "مستهلك جزئياً"  => all.Where(r => r.StatusText == "مستهلك جزئياً"),
                    "مستهلك كلياً"   => all.Where(r => r.StatusText == "مستهلك كلياً"),
                    "ملغي"           => all.Where(r => r.StatusText == "ملغي"),
                    _                => all
                };

                Reservations.Clear();
                foreach (var r in filtered)
                    Reservations.Add(r);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل الحجوزات: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
            {
                await LoadAsync();
                return;
            }

            try
            {
                var results = await _reservationService.SearchAsync(keyword);
                Reservations.Clear();
                foreach (var r in results)
                    Reservations.Add(r);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في البحث: {ex.Message}");
            }
        }

        private async Task LoadDetailAsync()
        {
            if (SelectedReservation == null)
            {
                Detail = null;
                return;
            }

            try
            {
                Detail = await _reservationService
                    .GetReservationByIdAsync(SelectedReservation.Id);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل التفاصيل: {ex.Message}");
            }
        }

        private async Task CancelReservationAsync()
        {
            if (SelectedReservation == null) return;

            var confirm = System.Windows.MessageBox.Show(
                $"هل تريد إلغاء الحجز {SelectedReservation.ReservationNumber}؟\n" +
                $"سيتم إرجاع الرصيد المتبقي: {SelectedReservation.AmountRemaining:N0} ر.ي فقط",
                "تأكيد الإلغاء",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning,
                System.Windows.MessageBoxResult.No,
                System.Windows.MessageBoxOptions.RightAlign |
                System.Windows.MessageBoxOptions.RtlReading);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;
                var result = await _reservationService
                    .CancelReservationAsync(SelectedReservation.Id);

                if (result.Success)
                {
                    ShowSuccess(result.Message);
                    await LoadAsync();
                }
                else
                {
                    ShowError(result.Message);
                }
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
    }
}
