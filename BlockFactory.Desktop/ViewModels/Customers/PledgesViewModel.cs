using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using BlockFactory.Core.DTOs.Customers;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;

namespace BlockFactory.Desktop.ViewModels.Customers
{
    public class PledgesViewModel : BaseViewModel
    {
        private readonly ICustomerService _customerService;

        public PledgesViewModel(ICustomerService customerService)
        {
            _customerService = customerService;
            InitializeCommands();
        }

        // ─── Collections ────────────────────────────
        public ObservableCollection<PledgeListDto> Pledges { get; }
            = new();

        private List<PledgeListDto> _allPledges = new();

        // ─── Properties ─────────────────────────────
        private string _selectedFilter = "النشطة";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                SetProperty(ref _selectedFilter, value);
                _ = LoadAsync();
            }
        }

        private PledgeListDto? _selectedPledge;
        public PledgeListDto? SelectedPledge
        {
            get => _selectedPledge;
            set => SetProperty(ref _selectedPledge, value);
        }

        // إحصائيات
        private int _activePledges;
        public int ActivePledges
        {
            get => _activePledges;
            set => SetProperty(ref _activePledges, value);
        }

        private int _overduePledges;
        public int OverduePledges
        {
            get => _overduePledges;
            set => SetProperty(ref _overduePledges, value);
        }

        private int _dueSoonPledges;
        public int DueSoonPledges
        {
            get => _dueSoonPledges;
            set => SetProperty(ref _dueSoonPledges, value);
        }

        public List<string> FilterOptions { get; } = new()
        {
            "النشطة",
            "المتأخرة",
            "الكل"
        };

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand LoadCommand { get; private set; } = null!;
        public AsyncRelayCommand ReturnPledgeCommand { get; private set; }
            = null!;

        private void InitializeCommands()
        {
            LoadCommand = new AsyncRelayCommand(
                async _ => await LoadAsync());

            ReturnPledgeCommand = new AsyncRelayCommand(
                async _ => await ReturnPledgeAsync(),
                _ => SelectedPledge != null &&
                     SelectedPledge.Status == "نشط");
        }

        public async Task LoadAsync()
        {
            try
            {
                IsLoading = true;

                IEnumerable<PledgeListDto> pledges = SelectedFilter switch
                {
                    "المتأخرة" => await _customerService
                        .GetOverduePledgesAsync(),
                    "النشطة" => await _customerService
                        .GetActivePledgesAsync(),
                    _ => await _customerService
                        .GetAllPledgesAsync()
                };

                _allPledges = pledges.ToList();

                Pledges.Clear();
                foreach (var p in _allPledges)
                    Pledges.Add(p);

                UpdateStats();
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

        private void UpdateStats()
        {
            ActivePledges = _allPledges
                .Count(p => p.Status == "نشط");
            OverduePledges = _allPledges
                .Count(p => p.IsOverdue);
            DueSoonPledges = _allPledges
                .Count(p => p.IsDueSoon && !p.IsOverdue);
        }

        private async Task ReturnPledgeAsync()
        {
            if (SelectedPledge == null) return;

            var confirm = MessageBox.Show(
                $"تأكيد استرجاع الرهن من العميل:\n" +
                $"{SelectedPledge.CustomerName}\n" +
                $"النوع: {SelectedPledge.PledgeType}\n" +
                $"الوصف: {SelectedPledge.Description}",
                "تأكيد استرجاع الرهن",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);

            if (confirm != MessageBoxResult.Yes) return;

            var result = await _customerService.ReturnPledgeAsync(
                new ReturnPledgeDto
                {
                    PledgeId = SelectedPledge.Id,
                    Notes = "تم الاسترجاع يدوياً"
                });

            if (result.Success)
            {
                ShowSuccess(result.Message);
                await LoadAsync();
            }
            else ShowError(result.Message);
        }
    }
}
