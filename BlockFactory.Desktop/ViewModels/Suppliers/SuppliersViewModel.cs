using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.DTOs.Suppliers;
using BlockFactory.Core.Interfaces.Services;
using System.Diagnostics;
using System.IO;
using BlockFactory.Core.Models.Suppliers;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;
using BlockFactory.Core.Common;

namespace BlockFactory.Desktop.ViewModels.Suppliers
{
    public class SuppliersViewModel : BaseViewModel
    {
        private readonly ISupplierService _supplierService;
        private readonly IReportService? _reportService;

        public SuppliersViewModel(
            ISupplierService supplierService,
            IReportService? reportService = null)
        {
            _supplierService = supplierService;
            _reportService = reportService;
            InitializeCommands();
        }

        // ─── Collections ────────────────────────────
        public ObservableCollection<SupplierListDto> Suppliers { get; }
            = new();

        private List<SupplierListDto> _allSuppliers = new();

        public ObservableCollection<SupplierInvoiceDto> Invoices { get; }
            = new();

        public ObservableCollection<RawMaterialLookupDto> RawMaterialOptions
        { get; } = new();

        public ObservableCollection<PurchaseInvoiceLineVm> PurchaseInvoiceLines
        { get; } = new();

        // ─── فاتورة شراء جديدة ─────────────────────
        private bool _isPurchaseInvoiceFormVisible;
        public bool IsPurchaseInvoiceFormVisible
        {
            get => _isPurchaseInvoiceFormVisible;
            set => SetProperty(ref _isPurchaseInvoiceFormVisible, value);
        }

        private string _purchaseInvoiceNumber = string.Empty;
        public string PurchaseInvoiceNumber
        {
            get => _purchaseInvoiceNumber;
            set => SetProperty(ref _purchaseInvoiceNumber, value);
        }

        private DateTime _purchaseInvoiceDate = DateTime.Today;
        public DateTime PurchaseInvoiceDate
        {
            get => _purchaseInvoiceDate;
            set => SetProperty(ref _purchaseInvoiceDate, value);
        }

        private DateTime? _purchaseDueDate;
        public DateTime? PurchaseDueDate
        {
            get => _purchaseDueDate;
            set => SetProperty(ref _purchaseDueDate, value);
        }

        private string _purchaseNotes = string.Empty;
        public string PurchaseNotes
        {
            get => _purchaseNotes;
            set => SetProperty(ref _purchaseNotes, value);
        }

        /// <summary>0 = آجل، 1 = دفع الآن.</summary>
        private int _purchasePaymentModeIndex;
        public int PurchasePaymentModeIndex
        {
            get => _purchasePaymentModeIndex;
            set
            {
                if (!SetProperty(ref _purchasePaymentModeIndex, value))
                    return;

                OnPropertyChanged(nameof(ShowPurchaseDueDate));
                OnPropertyChanged(nameof(ShowPurchasePayNow));
                if (value == 1)
                    PurchasePayNowAmount = PurchaseLinesTotal;
            }
        }

        private decimal _purchasePayNowAmount;
        public decimal PurchasePayNowAmount
        {
            get => _purchasePayNowAmount;
            set => SetProperty(ref _purchasePayNowAmount, value);
        }

        public bool ShowPurchaseDueDate => PurchasePaymentModeIndex == 0;

        public bool ShowPurchasePayNow => PurchasePaymentModeIndex == 1;

        public decimal PurchaseLinesTotal =>
            PurchaseInvoiceLines.Sum(l => l.Quantity * l.UnitPrice);

        private void NotifyPurchaseLineTotals()
        {
            OnPropertyChanged(nameof(PurchaseLinesTotal));
        }

        // ─── Summary ─────────────────────────────────
        private SuppliersSummaryDto? _summary;
        public SuppliersSummaryDto? Summary
        {
            get => _summary;
            set => SetProperty(ref _summary, value);
        }

        // ─── Selected ────────────────────────────────
        private SupplierListDto? _selectedSupplier;
        public SupplierListDto? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                SetProperty(ref _selectedSupplier, value);
                if (value != null)
                    _ = LoadInvoicesAsync(value.Id);
            }
        }

        private SupplierInvoiceDto? _selectedInvoice;
        public SupplierInvoiceDto? SelectedInvoice
        {
            get => _selectedInvoice;
            set => SetProperty(ref _selectedInvoice, value);
        }

        // ─── Search ──────────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterSuppliers();
            }
        }

        private string _selectedFilter = "الكل";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                SetProperty(ref _selectedFilter, value);
                FilterSuppliers();
            }
        }

        public List<string> FilterOptions { get; } = new()
        {
            "الكل",
            "لديهم ديون",
            "بدون ديون"
        };

        // ─── نموذج المورد ────────────────────────────
        private bool _isSupplierFormVisible;
        public bool IsSupplierFormVisible
        {
            get => _isSupplierFormVisible;
            set => SetProperty(ref _isSupplierFormVisible, value);
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
            ? "✏️ تعديل بيانات المورد"
            : "➕ إضافة مورد جديد";

        private string _formName = string.Empty;
        public string FormName
        {
            get => _formName;
            set => SetProperty(ref _formName, value);
        }

        private string _formCompany = string.Empty;
        public string FormCompany
        {
            get => _formCompany;
            set => SetProperty(ref _formCompany, value);
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

        private SupplierType _formType = SupplierType.Cement;
        public SupplierType FormType
        {
            get => _formType;
            set => SetProperty(ref _formType, value);
        }

        private string _formNotes = string.Empty;
        public string FormNotes
        {
            get => _formNotes;
            set => SetProperty(ref _formNotes, value);
        }

        // ─── نموذج الدفع ────────────────────────────
        private bool _isPayFormVisible;
        public bool IsPayFormVisible
        {
            get => _isPayFormVisible;
            set => SetProperty(ref _isPayFormVisible, value);
        }

        private decimal _payAmount;
        public decimal PayAmount
        {
            get => _payAmount;
            set => SetProperty(ref _payAmount, value);
        }

        private string? _payMethod;
        public string? PayMethod
        {
            get => _payMethod;
            set => SetProperty(ref _payMethod, value);
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

        // قوائم
        public List<SupplierTypeItem> SupplierTypes { get; } = new()
        {
            new("إسمنت 🏭", SupplierType.Cement),
            new("رمل 🏖️", SupplierType.Sand),
            new("حصى 🪨", SupplierType.Gravel),
            new("ماء 💧", SupplierType.Water),
            new("كهرباء ⚡", SupplierType.Electricity),
            new("أخرى 📦", SupplierType.Other)
        };

        public List<string> PaymentMethods { get; } = new()
        {
            "نقد",
            "كاش",
            "سبأفون",
            "وان كاش",
            "تحويل بنكي"
        };

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand LoadCommand { get; private set; } = null!;
        public RelayCommand ShowAddFormCommand { get; private set; } = null!;
        public RelayCommand ShowEditFormCommand { get; private set; } = null!;
        public AsyncRelayCommand SaveSupplierCommand { get; private set; }
            = null!;
        public RelayCommand CancelSupplierFormCommand { get; private set; }
            = null!;
        public RelayCommand ShowPayFormCommand { get; private set; } = null!;
        public RelayCommand CancelPayCommand { get; private set; } = null!;
        public AsyncRelayCommand SavePayCommand { get; private set; } = null!;
        public AsyncRelayCommand ShowNewPurchaseInvoiceCommand { get; private set; }
            = null!;
        public RelayCommand CancelPurchaseInvoiceCommand { get; private set; }
            = null!;
        public RelayCommand AddPurchaseLineCommand { get; private set; } = null!;
        public RelayCommand RemovePurchaseLineCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SavePurchaseInvoiceCommand { get; private set; }
            = null!;

        private void InitializeCommands()
        {
            LoadCommand = new AsyncRelayCommand(
                async _ => await LoadAsync());

            ShowAddFormCommand = new RelayCommand(_ =>
            {
                IsPurchaseInvoiceFormVisible = false;
                IsEditMode = false;
                ClearSupplierForm();
                IsSupplierFormVisible = true;
            });

            ShowEditFormCommand = new RelayCommand(
                _ =>
                {
                    if (SelectedSupplier == null) return;
                    IsPurchaseInvoiceFormVisible = false;
                    IsEditMode = true;
                    FormName = SelectedSupplier.FullName;
                    FormCompany = SelectedSupplier.CompanyName ?? "";
                    FormPhone = SelectedSupplier.Phone ?? "";
                    IsSupplierFormVisible = true;
                },
                _ => SelectedSupplier != null);

            SaveSupplierCommand = new AsyncRelayCommand(
                async _ => await SaveSupplierAsync(),
                _ => !string.IsNullOrWhiteSpace(FormName));

            CancelSupplierFormCommand = new RelayCommand(_ =>
            {
                IsSupplierFormVisible = false;
                IsPurchaseInvoiceFormVisible = false;
                ClearSupplierForm();
                ClearPurchaseInvoiceForm();
            });

            ShowPayFormCommand = new RelayCommand(
                _ =>
                {
                    if (SelectedSupplier == null) return;
                    IsPurchaseInvoiceFormVisible = false;
                    PayAmount = SelectedSupplier.TotalDebt;
                    PayMethod = "نقد";
                    PayReference = null;
                    PayNotes = null;
                    IsPayFormVisible = true;
                    ClearMessages();
                },
                _ => SelectedSupplier != null &&
                     SelectedSupplier.TotalDebt > 0);

            CancelPayCommand = new RelayCommand(_ =>
            {
                IsPayFormVisible = false;
                ClearMessages();
            });

            SavePayCommand = new AsyncRelayCommand(
                async _ => await SavePayAsync(),
                _ => SelectedSupplier != null && PayAmount > 0);

            ShowNewPurchaseInvoiceCommand = new AsyncRelayCommand(
                async _ => await ShowNewPurchaseInvoiceAsync(),
                _ => SelectedSupplier != null && !IsLoading);

            CancelPurchaseInvoiceCommand = new RelayCommand(_ =>
            {
                IsPurchaseInvoiceFormVisible = false;
                ClearPurchaseInvoiceForm();
                ClearMessages();
            });

            AddPurchaseLineCommand = new RelayCommand(
                _ =>
                {
                    PurchaseInvoiceLines.Add(
                        new PurchaseInvoiceLineVm(NotifyPurchaseLineTotals));
                    NotifyPurchaseLineTotals();
                },
                _ => IsPurchaseInvoiceFormVisible);

            RemovePurchaseLineCommand = new RelayCommand(
                param =>
                {
                    if (param is not PurchaseInvoiceLineVm line)
                        return;
                    if (PurchaseInvoiceLines.Count <= 1)
                        return;
                    PurchaseInvoiceLines.Remove(line);
                    NotifyPurchaseLineTotals();
                },
                _ => IsPurchaseInvoiceFormVisible &&
                     PurchaseInvoiceLines.Count > 1);

            SavePurchaseInvoiceCommand = new AsyncRelayCommand(
                async _ => await SavePurchaseInvoiceAsync());
        }

        // ─── Load ────────────────────────────────────
        public async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                Summary = await _supplierService.GetSummaryAsync();
                var list = await _supplierService.GetAllSuppliersAsync();
                _allSuppliers = list.ToList();
                FilterSuppliers();
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

        private async Task LoadInvoicesAsync(int supplierId)
        {
            try
            {
                var invoices = await _supplierService
                    .GetSupplierInvoicesAsync(supplierId);
                Invoices.Clear();
                foreach (var i in invoices)
                    Invoices.Add(i);
            }
            catch { }
        }

        private void FilterSuppliers()
        {
            IEnumerable<SupplierListDto> filtered =
                SelectedFilter switch
                {
                    "لديهم ديون" =>
                        _allSuppliers.Where(s => s.TotalDebt > 0),
                    "بدون ديون" =>
                        _allSuppliers.Where(s => s.TotalDebt <= 0),
                    _ => _allSuppliers
                };

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var kw = SearchText.ToLower();
                filtered = filtered.Where(s =>
                    s.FullName.ToLower().Contains(kw) ||
                    (s.Phone != null && s.Phone.Contains(kw)) ||
                    (s.CompanyName != null &&
                     s.CompanyName.ToLower().Contains(kw)));
            }

            Suppliers.Clear();
            foreach (var s in filtered)
                Suppliers.Add(s);
        }

        // ─── Save ────────────────────────────────────
        private async Task SaveSupplierAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                var dto = new CreateSupplierDto
                {
                    FullName = FormName,
                    CompanyName = FormCompany,
                    Phone = FormPhone,
                    Address = FormAddress,
                    SupplierType = FormType,
                    Notes = FormNotes
                };

                ServiceResult result;
                if (IsEditMode && SelectedSupplier != null)
                    result = await _supplierService
                        .UpdateSupplierAsync(SelectedSupplier.Id, dto);
                else
                    result = await _supplierService
                        .CreateSupplierAsync(dto);

                if (result.Success)
                {
                    ShowSuccess(result.Message);
                    IsSupplierFormVisible = false;
                    ClearSupplierForm();
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

        private async Task SavePayAsync()
        {
            if (SelectedSupplier == null) return;

            try
            {
                IsLoading = true;

                var result = await _supplierService.PaySupplierAsync(
                    new PaySupplierDto
                    {
                        SupplierId = SelectedSupplier.Id,
                        InvoiceId = SelectedInvoice?.Id,
                        Amount = PayAmount,
                        Method = PayMethod,
                        Reference = PayReference,
                        Notes = PayNotes
                    });

                if (result.Success)
                {
                    ShowSuccess(result.Message);
                    IsPayFormVisible = false;
                    await LoadAsync();
                    if (SelectedSupplier != null)
                        await LoadInvoicesAsync(SelectedSupplier.Id);
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

        private async Task ShowNewPurchaseInvoiceAsync()
        {
            if (SelectedSupplier == null)
                return;

            try
            {
                IsLoading = true;
                ClearMessages();
                IsSupplierFormVisible = false;
                IsPayFormVisible = false;

                RawMaterialOptions.Clear();
                foreach (var m in await _supplierService
                             .GetActiveRawMaterialsAsync())
                    RawMaterialOptions.Add(m);

                PurchaseInvoiceLines.Clear();
                PurchaseInvoiceLines.Add(
                    new PurchaseInvoiceLineVm(NotifyPurchaseLineTotals));

                PurchaseInvoiceNumber =
                    $"INV-SUP-{DateTime.Now:yyyyMMdd-HHmm}";
                PurchaseInvoiceDate = DateTime.Today;
                PurchaseDueDate = null;
                PurchaseNotes = string.Empty;
                PurchasePaymentModeIndex = 0;
                PurchasePayNowAmount = 0m;
                NotifyPurchaseLineTotals();
                IsPurchaseInvoiceFormVisible = true;
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل المواد: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SavePurchaseInvoiceAsync()
        {
            if (SelectedSupplier == null)
                return;

            if (string.IsNullOrWhiteSpace(PurchaseInvoiceNumber))
            {
                ShowError("يرجى إدخال رقم الفاتورة");
                return;
            }

            var items = new List<CreateSupplierInvoiceItemDto>();
            foreach (var line in PurchaseInvoiceLines)
            {
                if (line.SelectedMaterial == null ||
                    line.Quantity <= 0)
                {
                    ShowError("يرجى اختيار مادة وكمية صحيحة لكل سطر");
                    return;
                }

                if (line.UnitPrice < 0)
                {
                    ShowError("سعر الوحدة لا يمكن أن يكون سالباً");
                    return;
                }

                items.Add(new CreateSupplierInvoiceItemDto
                {
                    RawMaterialId = line.SelectedMaterial.Id,
                    Description = line.SelectedMaterial.Name,
                    Quantity = line.Quantity,
                    Unit = line.SelectedMaterial.UnitAr,
                    UnitPrice = line.UnitPrice
                });
            }

            if (items.Count == 0)
            {
                ShowError("أضف بنداً واحداً على الأقل");
                return;
            }

            var linesTotal = PurchaseLinesTotal;
            if (PurchasePaymentModeIndex == 1)
            {
                if (PurchasePayNowAmount <= 0)
                {
                    ShowError("أدخل مبلغ الشراء المدفوع الآن (أكبر من صفر)");
                    return;
                }

                if (PurchasePayNowAmount > linesTotal)
                {
                    ShowError("مبلغ الدفع الآن لا يمكن أن يتجاوز إجمالي البنود");
                    return;
                }
            }

            try
            {
                IsLoading = true;
                ClearMessages();

                var dto = new CreateSupplierInvoiceDto
                {
                    SupplierId = SelectedSupplier.Id,
                    InvoiceNumber = PurchaseInvoiceNumber.Trim(),
                    InvoiceDate = PurchaseInvoiceDate,
                    DueDate = PurchaseDueDate,
                    Notes = string.IsNullOrWhiteSpace(PurchaseNotes)
                        ? null
                        : PurchaseNotes.Trim(),
                    IsCredit = PurchasePaymentModeIndex == 0,
                    PayNowAmount = PurchasePaymentModeIndex == 1
                        ? PurchasePayNowAmount
                        : 0m,
                    Items = items
                };

                var result = await _supplierService.CreateInvoiceAsync(dto);

                if (result.Success)
                {
                    ShowSuccess(result.Message);
                    var invoiceId = result.Data;
                    var supplierId = SelectedSupplier.Id;
                    IsPurchaseInvoiceFormVisible = false;
                    ClearPurchaseInvoiceForm();
                    await LoadAsync();
                    var sel = Suppliers.FirstOrDefault(s => s.Id == supplierId);
                    if (sel != null)
                        SelectedSupplier = sel;
                    else
                        await LoadInvoicesAsync(supplierId);

                    // ─── سؤال الطباعة ────────────────────
                    var print = MessageBox.Show(
                        "هل تريد طباعة فاتورة الشراء؟",
                        "طباعة الفاتورة",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.Yes,
                        MessageBoxOptions.RightAlign |
                        MessageBoxOptions.RtlReading);

                    if (print == MessageBoxResult.Yes)
                        await PrintSupplierInvoiceAsync(invoiceId);
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
            }
        }

        private async Task PrintSupplierInvoiceAsync(int invoiceId)
        {
            try
            {
                if (_reportService == null)
                {
                    ShowError("خدمة التقارير غير متوفرة");
                    return;
                }

                IsLoading = true;
                var pdfBytes = await _reportService
                    .GenerateSupplierInvoicePdfAsync(invoiceId);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    ShowError("لم يتم إنشاء ملف PDF");
                    return;
                }

                // حفظ مؤقت وفتح للطباعة
                var tempPath = Path.Combine(
                    Path.GetTempPath(),
                    $"SupplierInvoice_{invoiceId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

                await File.WriteAllBytesAsync(tempPath, pdfBytes);

                // فتح PDF في المتصفح/Adobe للطباعة
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في الطباعة: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearPurchaseInvoiceForm()
        {
            PurchaseInvoiceLines.Clear();
            PurchaseInvoiceNumber = string.Empty;
            PurchaseInvoiceDate = DateTime.Today;
            PurchaseDueDate = null;
            PurchaseNotes = string.Empty;
            PurchasePaymentModeIndex = 0;
            PurchasePayNowAmount = 0m;
        }

        private void ClearSupplierForm()
        {
            FormName = string.Empty;
            FormCompany = string.Empty;
            FormPhone = string.Empty;
            FormAddress = string.Empty;
            FormType = SupplierType.Cement;
            FormNotes = string.Empty;
            ClearMessages();
        }
    }

    /// <summary>سطر في نموذج فاتورة شراء (واجهة فقط).</summary>
    public class PurchaseInvoiceLineVm : BaseViewModel
    {
        private readonly Action? _onLineChanged;

        public PurchaseInvoiceLineVm(Action? onLineChanged = null)
        {
            _onLineChanged = onLineChanged;
        }

        private void NotifyLineTotals()
        {
            _onLineChanged?.Invoke();
        }

        private RawMaterialLookupDto? _selectedMaterial;
        public RawMaterialLookupDto? SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                if (!SetProperty(ref _selectedMaterial, value))
                    return;

                NotifyLineTotals();
            }
        }

        private decimal _quantity = 1;
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (!SetProperty(ref _quantity, value))
                    return;

                NotifyLineTotals();
            }
        }

        private decimal _unitPrice;
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (!SetProperty(ref _unitPrice, value))
                    return;

                NotifyLineTotals();
            }
        }
    }

    public record SupplierTypeItem(string Name, SupplierType Value);
}