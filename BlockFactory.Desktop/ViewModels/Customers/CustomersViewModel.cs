using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.DTOs.Customers;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Session;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;

namespace BlockFactory.Desktop.ViewModels.Customers
{
    public class CustomersViewModel : BaseViewModel
    {
        private readonly ICustomerService _customerService;

        public CustomersViewModel(ICustomerService customerService)
        {
            _customerService = customerService;
            InitializeCommands();
        }

        // ─── Collections ────────────────────────────
        public ObservableCollection<CustomerListDto> Customers { get; }
            = new();

        private List<CustomerListDto> _allCustomers = new();

        // ─── Properties ─────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterCustomers();
            }
        }

        private string _selectedFilter = "الكل";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                SetProperty(ref _selectedFilter, value);
                FilterCustomers();
            }
        }

        private CustomerListDto? _selectedCustomer;
        public CustomerListDto? SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        // نموذج الإضافة/التعديل
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
            ? "✏️ تعديل بيانات العميل"
            : "➕ إضافة عميل جديد";

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

        private string _formNotes = string.Empty;
        public string FormNotes
        {
            get => _formNotes;
            set => SetProperty(ref _formNotes, value);
        }

        // إحصائيات
        private int _totalCustomers;
        public int TotalCustomers
        {
            get => _totalCustomers;
            set => SetProperty(ref _totalCustomers, value);
        }

        private decimal _totalDebt;
        public decimal TotalDebt
        {
            get => _totalDebt;
            set => SetProperty(ref _totalDebt, value);
        }

        private int _customersWithDebt;
        public int CustomersWithDebt
        {
            get => _customersWithDebt;
            set => SetProperty(ref _customersWithDebt, value);
        }

        // فلاتر
        public List<string> FilterOptions { get; } = new()
        {
            "الكل",
            "لديهم ديون",
            "بدون ديون",
            "لديهم رهن"
        };

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand LoadCommand { get; private set; } = null!;
        public RelayCommand ShowAddFormCommand { get; private set; } = null!;
        public RelayCommand ShowEditFormCommand { get; private set; } = null!;
        public AsyncRelayCommand SaveCommand { get; private set; } = null!;
        public RelayCommand CancelFormCommand { get; private set; } = null!;
        public AsyncRelayCommand DeleteCommand { get; private set; } = null!;
        public AsyncRelayCommand ViewDetailCommand { get; private set; } = null!;

        public bool CanDeleteCustomer =>
            CurrentSession.Instance.HasPermission("DeleteCustomer");

        private void InitializeCommands()
        {
            LoadCommand = new AsyncRelayCommand(
                async _ => await LoadAsync());

            ShowAddFormCommand = new RelayCommand(_ =>
            {
                IsEditMode = false;
                ClearForm();
                IsFormVisible = true;
            });

            ShowEditFormCommand = new RelayCommand(
                _ =>
                {
                    if (SelectedCustomer == null) return;
                    IsEditMode = true;
                    FormName = SelectedCustomer.FullName;
                    FormPhone = SelectedCustomer.Phone ?? "";
                    FormAddress = SelectedCustomer.Address ?? "";
                    IsFormVisible = true;
                },
                _ => SelectedCustomer != null);

            SaveCommand = new AsyncRelayCommand(
                async _ => await SaveAsync(),
                _ => !string.IsNullOrWhiteSpace(FormName));

            CancelFormCommand = new RelayCommand(_ =>
            {
                IsFormVisible = false;
                ClearForm();
            });

            DeleteCommand = new AsyncRelayCommand(
                async _ => await DeleteAsync(),
                _ => SelectedCustomer != null &&
                     CurrentSession.Instance.HasPermission("DeleteCustomer"));

            ViewDetailCommand = new AsyncRelayCommand(
                async _ => await ViewDetailAsync(),
                _ => SelectedCustomer != null);
        }

        // ─── Load ────────────────────────────────────
        public async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                var list = await _customerService.GetAllCustomersAsync();
                _allCustomers = list.ToList();
                FilterCustomers();
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

        private void FilterCustomers()
        {
            IEnumerable<CustomerListDto> filtered = SelectedFilter switch
            {
                "لديهم ديون" => _allCustomers.Where(c => c.TotalDebt > 0),
                "بدون ديون" => _allCustomers.Where(c => c.TotalDebt <= 0),
                "لديهم رهن" => _allCustomers.Where(c => c.HasActivePledge),
                _ => _allCustomers
            };

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var kw = SearchText.ToLower();
                filtered = filtered.Where(c =>
                    c.FullName.ToLower().Contains(kw) ||
                    (c.Phone != null && c.Phone.Contains(kw)));
            }

            Customers.Clear();
            foreach (var c in filtered)
                Customers.Add(c);

            UpdateStats();
        }

        private void UpdateStats()
        {
            TotalCustomers = Customers.Count;
            TotalDebt = Customers.Sum(c => c.TotalDebt);
            CustomersWithDebt = Customers.Count(c => c.TotalDebt > 0);
        }

        // ─── Save ────────────────────────────────────
        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                if (IsEditMode && SelectedCustomer != null)
                {
                    var result = await _customerService.UpdateCustomerAsync(
                        new UpdateCustomerDto
                        {
                            Id = SelectedCustomer.Id,
                            FullName = FormName,
                            Phone = FormPhone,
                            Address = FormAddress,
                            Notes = FormNotes
                        });

                    if (result.Success)
                    {
                        ShowSuccess(result.Message);
                        IsFormVisible = false;
                        await LoadAsync();
                    }
                    else ShowError(result.Message);
                }
                else
                {
                    var result = await _customerService.CreateCustomerAsync(
                        new CreateCustomerDto
                        {
                            FullName = FormName,
                            Phone = FormPhone,
                            Address = FormAddress,
                            Notes = FormNotes
                        });

                    if (result.Success)
                    {
                        ShowSuccess(result.Message);
                        IsFormVisible = false;
                        ClearForm();
                        await LoadAsync();
                    }
                    else ShowError(result.Message);
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

        private async Task DeleteAsync()
        {
            if (SelectedCustomer == null) return;

            var confirm = MessageBox.Show(
                $"هل تريد حذف العميل '{SelectedCustomer.FullName}'؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);

            if (confirm != MessageBoxResult.Yes) return;

            var result = await _customerService
                .DeleteCustomerAsync(SelectedCustomer.Id);

            if (result.Success)
            {
                ShowSuccess(result.Message);
                await LoadAsync();
            }
            else ShowError(result.Message);
        }

        private async Task ViewDetailAsync()
        {
            if (SelectedCustomer == null) return;

            var detail = await _customerService
                .GetCustomerDetailAsync(SelectedCustomer.Id);

            if (detail == null) return;

            // سيتم فتح نافذة التفاصيل لاحقاً
            await Task.CompletedTask;
        }

        private void ClearForm()
        {
            FormName = string.Empty;
            FormPhone = string.Empty;
            FormAddress = string.Empty;
            FormNotes = string.Empty;
            ClearMessages();
        }
    }
}
