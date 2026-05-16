using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.DTOs.HR;
using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;
using BlockFactory.Core.Common;
using BlockFactory.Core.Session;

namespace BlockFactory.Desktop.ViewModels.HR
{
    public class WorkersViewModel : BaseViewModel
    {
        private readonly IHRService _hrService;

        public WorkersViewModel(IHRService hrService)
        {
            _hrService = hrService;
            InitializeCommands();
        }

        public bool CanManageWorkers =>
            CurrentSession.Instance.HasPermission("ManageHR");

        public bool CanAddAdvance =>
            CurrentSession.Instance.HasPermission("AddAdvance");

        // ─── Collections ────────────────────────────
        public ObservableCollection<WorkerListDto> Workers { get; }
            = new();

        private List<WorkerListDto> _allWorkers = new();

        // ─── Properties ─────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterWorkers();
            }
        }

        private WorkerListDto? _selectedWorker;
        public WorkerListDto? SelectedWorker
        {
            get => _selectedWorker;
            set => SetProperty(ref _selectedWorker, value);
        }

        // نموذج العامل
        private bool _isFormVisible;
        public bool IsFormVisible
        {
            get => _isFormVisible;
            set => SetProperty(ref _isFormVisible, value);
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                SetProperty(ref _isEditMode, value);
                OnPropertyChanged(nameof(FormTitle));
            }
        }

        public string FormTitle => IsEditMode
            ? "✏️ تعديل بيانات العامل"
            : "➕ إضافة عامل جديد";

        private string _formName = string.Empty;
        public string FormName
        {
            get => _formName;
            set => SetProperty(ref _formName, value);
        }

        private string _formPhone = string.Empty;
        public string FormPhone
        {
            get => _formPhone;
            set => SetProperty(ref _formPhone, value);
        }

        private string _formAddress = string.Empty;
        public string FormAddress
        {
            get => _formAddress;
            set => SetProperty(ref _formAddress, value);
        }

        private string _formNationalId = string.Empty;
        public string FormNationalId
        {
            get => _formNationalId;
            set => SetProperty(ref _formNationalId, value);
        }

        private decimal _formSalary;
        public decimal FormSalary
        {
            get => _formSalary;
            set => SetProperty(ref _formSalary, value);
        }

        private DateTime _formHireDate = DateTime.Today;
        public DateTime FormHireDate
        {
            get => _formHireDate;
            set => SetProperty(ref _formHireDate, value);
        }

        private string _formNotes = string.Empty;
        public string FormNotes
        {
            get => _formNotes;
            set => SetProperty(ref _formNotes, value);
        }

        // نموذج السلفة
        private bool _isAdvanceFormVisible;
        public bool IsAdvanceFormVisible
        {
            get => _isAdvanceFormVisible;
            set => SetProperty(ref _isAdvanceFormVisible, value);
        }

        private decimal _advanceAmount;
        public decimal AdvanceAmount
        {
            get => _advanceAmount;
            set => SetProperty(ref _advanceAmount, value);
        }

        private string _advanceReason = string.Empty;
        public string AdvanceReason
        {
            get => _advanceReason;
            set => SetProperty(ref _advanceReason, value);
        }

        // إحصائيات
        private int _totalWorkers;
        public int TotalWorkers
        {
            get => _totalWorkers;
            set => SetProperty(ref _totalWorkers, value);
        }

        private int _activeWorkers;
        public int ActiveWorkers
        {
            get => _activeWorkers;
            set => SetProperty(ref _activeWorkers, value);
        }

        private decimal _totalSalaries;
        public decimal TotalSalaries
        {
            get => _totalSalaries;
            set => SetProperty(ref _totalSalaries, value);
        }

        private decimal _totalPendingAdvances;
        public decimal TotalPendingAdvances
        {
            get => _totalPendingAdvances;
            set => SetProperty(ref _totalPendingAdvances, value);
        }

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand LoadCommand { get; private set; } = null!;
        public RelayCommand ShowAddFormCommand { get; private set; } = null!;
        public RelayCommand ShowEditFormCommand { get; private set; } = null!;
        public AsyncRelayCommand SaveWorkerCommand { get; private set; }
            = null!;
        public RelayCommand CancelFormCommand { get; private set; } = null!;
        public AsyncRelayCommand DeactivateCommand { get; private set; }
            = null!;
        public RelayCommand ShowAdvanceFormCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SaveAdvanceCommand { get; private set; }
            = null!;
        public RelayCommand CancelAdvanceCommand { get; private set; }
            = null!;

        private void InitializeCommands()
        {
            LoadCommand = new AsyncRelayCommand(
                async _ => await LoadAsync());

            ShowAddFormCommand = new RelayCommand(_ =>
            {
                IsEditMode = false;
                ClearWorkerForm();
                IsFormVisible = true;
            },
            _ => CurrentSession.Instance.HasPermission("ManageHR"));

            ShowEditFormCommand = new RelayCommand(
                _ =>
                {
                    if (SelectedWorker == null) return;
                    IsEditMode = true;
                    FormName = SelectedWorker.FullName;
                    FormPhone = SelectedWorker.Phone ?? "";
                    FormNationalId = SelectedWorker.NationalId ?? "";
                    FormSalary = SelectedWorker.BasicSalary;
                    FormHireDate = SelectedWorker.HireDate;
                    IsFormVisible = true;
                },
                _ => SelectedWorker != null &&
                     CurrentSession.Instance.HasPermission("ManageHR"));

            SaveWorkerCommand = new AsyncRelayCommand(
                async _ => await SaveWorkerAsync(),
                _ => !string.IsNullOrWhiteSpace(FormName) &&
                     FormSalary > 0 &&
                     CurrentSession.Instance.HasPermission("ManageHR"));

            CancelFormCommand = new RelayCommand(_ =>
            {
                IsFormVisible = false;
                ClearWorkerForm();
            });

            DeactivateCommand = new AsyncRelayCommand(
                async _ => await DeactivateAsync(),
                _ => SelectedWorker != null &&
                     SelectedWorker.Status == "نشط" &&
                     CurrentSession.Instance.HasPermission("ManageHR"));

            ShowAdvanceFormCommand = new RelayCommand(
                _ =>
                {
                    if (SelectedWorker == null) return;
                    AdvanceAmount = 0;
                    AdvanceReason = string.Empty;
                    IsAdvanceFormVisible = true;
                },
                _ => SelectedWorker != null &&
                     SelectedWorker.Status == "نشط" &&
                     CurrentSession.Instance.HasPermission("AddAdvance"));

            SaveAdvanceCommand = new AsyncRelayCommand(
                async _ => await SaveAdvanceAsync(),
                _ => SelectedWorker != null &&
                     AdvanceAmount > 0 &&
                     CurrentSession.Instance.HasPermission("AddAdvance"));

            CancelAdvanceCommand = new RelayCommand(_ =>
            {
                IsAdvanceFormVisible = false;
            });
        }

        // ─── Load ────────────────────────────────────
        public async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                var list = await _hrService.GetAllWorkersAsync();
                _allWorkers = list.ToList();
                FilterWorkers();
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

        private void FilterWorkers()
        {
            IEnumerable<WorkerListDto> filtered = _allWorkers;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var kw = SearchText.ToLower();
                filtered = filtered.Where(w =>
                    w.FullName.ToLower().Contains(kw) ||
                    (w.Phone != null && w.Phone.Contains(kw)));
            }

            Workers.Clear();
            foreach (var w in filtered)
                Workers.Add(w);

            UpdateStats();
        }

        private void UpdateStats()
        {
            TotalWorkers = Workers.Count;
            ActiveWorkers = Workers.Count(w => w.Status == "نشط");
            TotalSalaries = Workers.Sum(w => w.BasicSalary);
            TotalPendingAdvances = Workers.Sum(w => w.PendingAdvances);
        }

        // ─── Save Worker ─────────────────────────────
        private async Task SaveWorkerAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                ServiceResult result;

                if (IsEditMode && SelectedWorker != null)
                {
                    result = await _hrService.UpdateWorkerAsync(
                        new UpdateWorkerDto
                        {
                            Id = SelectedWorker.Id,
                            FullName = FormName,
                            Phone = FormPhone,
                            Address = FormAddress,
                            NationalId = FormNationalId,
                            BasicSalary = FormSalary,
                            HireDate = FormHireDate,
                            Notes = FormNotes
                        });
                }
                else
                {
                    result = (await _hrService.CreateWorkerAsync(
                        new CreateWorkerDto
                        {
                            FullName = FormName,
                            Phone = FormPhone,
                            Address = FormAddress,
                            NationalId = FormNationalId,
                            BasicSalary = FormSalary,
                            HireDate = FormHireDate,
                            Notes = FormNotes
                        }));
                }

                if (result.Success)
                {
                    ShowSuccess(result.Message);
                    IsFormVisible = false;
                    ClearWorkerForm();
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

        private async Task DeactivateAsync()
        {
            if (SelectedWorker == null) return;

            var confirm = MessageBox.Show(
                $"هل تريد إيقاف العامل '{SelectedWorker.FullName}'؟",
                "تأكيد الإيقاف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);

            if (confirm != MessageBoxResult.Yes) return;

            var result = await _hrService
                .DeactivateWorkerAsync(SelectedWorker.Id);

            if (result.Success)
            {
                ShowSuccess(result.Message);
                await LoadAsync();
            }
            else ShowError(result.Message);
        }

        // ─── Save Advance ────────────────────────────
        private async Task SaveAdvanceAsync()
        {
            if (SelectedWorker == null) return;

            try
            {
                IsLoading = true;

                var result = await _hrService.AddAdvanceAsync(
                    new CreateAdvanceDto
                    {
                        WorkerId = SelectedWorker.Id,
                        Amount = AdvanceAmount,
                        AdvanceDate = DateTime.Today,
                        Reason = AdvanceReason
                    });

                if (result.Success)
                {
                    ShowSuccess(result.Message);
                    IsAdvanceFormVisible = false;
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

        private void ClearWorkerForm()
        {
            FormName = string.Empty;
            FormPhone = string.Empty;
            FormAddress = string.Empty;
            FormNationalId = string.Empty;
            FormSalary = 0;
            FormHireDate = DateTime.Today;
            FormNotes = string.Empty;
            ClearMessages();
        }
    }
}
