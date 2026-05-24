/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.Services;
using BlockFactory.Desktop.ViewModels.Base;
using BlockFactory.Core.Session;
using System.Collections.ObjectModel;
using System.Windows;

namespace BlockFactory.Desktop.ViewModels.Settings
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ApiHostService _apiHostService;
        private readonly BackupService _backupService;
        private readonly CloudSyncService _cloudSyncService;

        public SettingsViewModel(
            ApiHostService apiHostService,
            BackupService backupService,
            CloudSyncService cloudSyncService)
        {
            _apiHostService = apiHostService;
            _backupService = backupService;
            _cloudSyncService = cloudSyncService;

            _cloudSyncService.OnSyncStatusChanged +=
                status => SyncStatus = status;

            InitializeCommands();
            LoadBackupList();
        }

        public bool CanManageApi =>
            CurrentSession.Instance.HasPermission("ManageApi");

        public bool CanManageBackup =>
            CurrentSession.Instance.HasPermission("ManageBackup");

        public bool CanManageCloudSettings =>
            CurrentSession.Instance.HasPermission("ManageSettings");

        // ─── API Status ──────────────────────────────
        private bool _isApiRunning;
        public bool IsApiRunning
        {
            get => _isApiRunning;
            set
            {
                SetProperty(ref _isApiRunning, value);
                OnPropertyChanged(nameof(ApiStatusText));
                OnPropertyChanged(nameof(ApiStatusColor));
                OnPropertyChanged(nameof(ApiToggleText));
            }
        }

        private string _apiUrl = string.Empty;
        public string ApiUrl
        {
            get => _apiUrl;
            set => SetProperty(ref _apiUrl, value);
        }

        public string ApiStatusText => IsApiRunning
            ? "🟢 يعمل" : "🔴 متوقف";

        public string ApiStatusColor => IsApiRunning
            ? "#27AE60" : "#E74C3C";

        public string ApiToggleText => IsApiRunning
            ? "⛔ إيقاف API" : "▶️ تشغيل API";

        // ─── Sync Status ─────────────────────────────
        private string _syncStatus = "غير متصل";
        public string SyncStatus
        {
            get => _syncStatus;
            set => SetProperty(ref _syncStatus, value);
        }

        private bool _isSyncEnabled;
        public bool IsSyncEnabled
        {
            get => _isSyncEnabled;
            set
            {
                SetProperty(ref _isSyncEnabled, value);
                if (value)
                    _cloudSyncService.Enable();
                else
                    _cloudSyncService.Disable();
            }
        }

        // ─── Backup ──────────────────────────────────
        public ObservableCollection<BackupInfo> BackupList { get; }
            = new();

        private BackupInfo? _selectedBackup;
        public BackupInfo? SelectedBackup
        {
            get => _selectedBackup;
            set => SetProperty(ref _selectedBackup, value);
        }

        private string _backupStatus = string.Empty;
        public string BackupStatus
        {
            get => _backupStatus;
            set => SetProperty(ref _backupStatus, value);
        }

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand ToggleApiCommand { get; private set; }
            = null!;
        public AsyncRelayCommand ManualSyncCommand { get; private set; }
            = null!;
        public AsyncRelayCommand CreateBackupCommand { get; private set; }
            = null!;
        public AsyncRelayCommand BackupToUsbCommand { get; private set; }
            = null!;
        public AsyncRelayCommand RestoreBackupCommand { get; private set; }
            = null!;
        public RelayCommand OpenBackupFolderCommand { get; private set; }
            = null!;

        private void InitializeCommands()
        {
            ToggleApiCommand = new AsyncRelayCommand(
                async _ => await ToggleApiAsync(),
                _ => CurrentSession.Instance.HasPermission("ManageApi"));

            ManualSyncCommand = new AsyncRelayCommand(
                async _ =>
                {
                    if (!CurrentSession.Instance.HasPermission("ManageSettings"))
                        return;
                    SyncStatus = "جاري المزامنة...";
                    var success = await _cloudSyncService.SyncAsync();
                    SyncStatus = success
                        ? $"✅ تمت المزامنة: " +
                          $"{DateTime.Now:HH:mm}"
                        : "❌ فشلت المزامنة";
                },
                _ => CurrentSession.Instance.HasPermission("ManageSettings"));

            CreateBackupCommand = new AsyncRelayCommand(
                async _ => await CreateBackupAsync(),
                _ => CurrentSession.Instance.HasPermission("ManageBackup"));

            BackupToUsbCommand = new AsyncRelayCommand(
                async _ => await BackupToUsbAsync(),
                _ => CurrentSession.Instance.HasPermission("ManageBackup"));

            RestoreBackupCommand = new AsyncRelayCommand(
                async _ => await RestoreBackupAsync(),
                _ => SelectedBackup != null &&
                     CurrentSession.Instance.HasPermission("ManageBackup"));

            OpenBackupFolderCommand = new RelayCommand(_ =>
            {
                if (!CurrentSession.Instance.HasPermission("ManageBackup"))
                    return;
                var folder = System.IO.Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments),
                    "BlockFactory_Backups");

                System.Diagnostics.Process.Start(
                    "explorer.exe", folder);
            },
            _ => CurrentSession.Instance.HasPermission("ManageBackup"));
        }

        // ─── API Toggle ──────────────────────────────
        private async Task ToggleApiAsync()
        {
            try
            {
                IsLoading = true;

                if (IsApiRunning)
                {
                    await _apiHostService.StopAsync();
                    IsApiRunning = false;
                    ApiUrl = string.Empty;
                    ShowSuccess("تم إيقاف الـ API");
                }
                else
                {
                    await _apiHostService.StartAsync();
                    IsApiRunning = true;
                    ApiUrl = _apiHostService.ApiUrl;
                    ShowSuccess(
                        $"الـ API يعمل على: {ApiUrl}");
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

        // ─── Backup Operations ───────────────────────
        private async Task CreateBackupAsync()
        {
            try
            {
                IsLoading = true;
                BackupStatus = "⏳ جاري إنشاء النسخة الاحتياطية...";

                var result = await _backupService.CreateBackupAsync();

                if (result.Success)
                {
                    BackupStatus =
                        $"✅ تم إنشاء النسخة: {result.SizeText}";
                    LoadBackupList();
                }
                else
                {
                    BackupStatus = $"❌ فشل: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                BackupStatus = $"❌ خطأ: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task BackupToUsbAsync()
        {
            try
            {
                IsLoading = true;
                BackupStatus = "⏳ جاري النسخ على USB...";

                var result = await _backupService.BackupToUsbAsync();

                BackupStatus = result.Success
                    ? $"✅ تم النسخ على USB: {result.SizeText}"
                    : $"❌ {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                BackupStatus = $"❌ خطأ: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RestoreBackupAsync()
        {
            if (SelectedBackup == null) return;

            var confirm = MessageBox.Show(
                $"⚠️ تحذير: سيتم استعادة النسخة الاحتياطية\n" +
                $"وسيتم فقدان جميع التغييرات الحالية!\n\n" +
                $"الملف: {SelectedBackup.FileName}\n" +
                $"التاريخ: {SelectedBackup.DateText}\n\n" +
                $"هل أنت متأكد؟",
                "تأكيد الاستعادة",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;
                BackupStatus = "⏳ جاري الاستعادة...";

                var result = await _backupService
                    .RestoreBackupAsync(SelectedBackup.FullPath);

                if (result.Success)
                {
                    BackupStatus = "✅ تمت الاستعادة بنجاح";
                    MessageBox.Show(
                        "تمت استعادة قاعدة البيانات بنجاح.\n" +
                        "يرجى إعادة تشغيل التطبيق.",
                        "تمت الاستعادة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information,
                        MessageBoxResult.OK,
                        MessageBoxOptions.RightAlign |
                        MessageBoxOptions.RtlReading);
                }
                else
                {
                    BackupStatus = $"❌ {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                BackupStatus = $"❌ خطأ: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadBackupList()
        {
            BackupList.Clear();
            foreach (var backup in _backupService.GetBackupList())
                BackupList.Add(backup);
        }
    }
}*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.Services;
using BlockFactory.Desktop.ViewModels.Base;
using BlockFactory.Core.Session;
using BlockFactory.Core.Interfaces.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace BlockFactory.Desktop.ViewModels.Settings
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ApiHostService _apiHostService;
        private readonly BackupService _backupService;
        private readonly CloudSyncService _cloudSyncService;
        private readonly IProductService _productService;

        public SettingsViewModel(
            ApiHostService apiHostService,
            BackupService backupService,
            CloudSyncService cloudSyncService,
            IProductService productService)
        {
            _apiHostService = apiHostService;
            _backupService = backupService;
            _cloudSyncService = cloudSyncService;
            _productService = productService;

            _cloudSyncService.OnSyncStatusChanged +=
                status => SyncStatus = status;

            InitializeCommands();
            LoadBackupList();
            _ = LoadProductsAsync();
        }

        public bool CanManageApi =>
            CurrentSession.Instance.HasPermission("ManageApi");

        public bool CanManageBackup =>
            CurrentSession.Instance.HasPermission("ManageBackup");

        public bool CanManageCloudSettings =>
            CurrentSession.Instance.HasPermission("ManageSettings");

        // ─── API Status ──────────────────────────────
        private bool _isApiRunning;
        public bool IsApiRunning
        {
            get => _isApiRunning;
            set
            {
                SetProperty(ref _isApiRunning, value);
                OnPropertyChanged(nameof(ApiStatusText));
                OnPropertyChanged(nameof(ApiStatusColor));
                OnPropertyChanged(nameof(ApiToggleText));
            }
        }

        private string _apiUrl = string.Empty;
        public string ApiUrl
        {
            get => _apiUrl;
            set => SetProperty(ref _apiUrl, value);
        }

        public string ApiStatusText => IsApiRunning
            ? "🟢 يعمل" : "🔴 متوقف";

        public string ApiStatusColor => IsApiRunning
            ? "#27AE60" : "#E74C3C";

        public string ApiToggleText => IsApiRunning
            ? "⛔ إيقاف API" : "▶️ تشغيل API";

        // ─── Sync Status ─────────────────────────────
        private string _syncStatus = "غير متصل";
        public string SyncStatus
        {
            get => _syncStatus;
            set => SetProperty(ref _syncStatus, value);
        }

        private bool _isSyncEnabled;
        public bool IsSyncEnabled
        {
            get => _isSyncEnabled;
            set
            {
                SetProperty(ref _isSyncEnabled, value);
                if (value)
                    _cloudSyncService.Enable();
                else
                    _cloudSyncService.Disable();
            }
        }

        // ─── Products ────────────────────────────────
        public ObservableCollection<ProductSettingsRow> Products { get; } = new();

        private ProductSettingsRow? _selectedProduct;
        public ProductSettingsRow? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                SetProperty(ref _selectedProduct, value);
                if (value != null)
                {
                    FormPriceMin = value.PriceMin;
                    FormPriceMax = value.PriceMax;
                    FormDefaultPrice = value.DefaultPrice;
                    FormMinThreshold = value.MinThreshold;
                    IsFormVisible = true;
                    ClearMessages();
                }
            }
        }

        private bool _isFormVisible;
        public bool IsFormVisible
        {
            get => _isFormVisible;
            set => SetProperty(ref _isFormVisible, value);
        }

        private decimal _formPriceMin;
        public decimal FormPriceMin
        {
            get => _formPriceMin;
            set => SetProperty(ref _formPriceMin, value);
        }

        private decimal _formPriceMax;
        public decimal FormPriceMax
        {
            get => _formPriceMax;
            set => SetProperty(ref _formPriceMax, value);
        }

        private decimal _formDefaultPrice;
        public decimal FormDefaultPrice
        {
            get => _formDefaultPrice;
            set => SetProperty(ref _formDefaultPrice, value);
        }

        private int _formMinThreshold;
        public int FormMinThreshold
        {
            get => _formMinThreshold;
            set => SetProperty(ref _formMinThreshold, value);
        }

        // ─── Backup ──────────────────────────────────
        public ObservableCollection<BackupInfo> BackupList { get; }
            = new();

        private BackupInfo? _selectedBackup;
        public BackupInfo? SelectedBackup
        {
            get => _selectedBackup;
            set => SetProperty(ref _selectedBackup, value);
        }

        private string _backupStatus = string.Empty;
        public string BackupStatus
        {
            get => _backupStatus;
            set => SetProperty(ref _backupStatus, value);
        }

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand ToggleApiCommand { get; private set; }
            = null!;
        public AsyncRelayCommand ManualSyncCommand { get; private set; }
            = null!;
        public AsyncRelayCommand CreateBackupCommand { get; private set; }
            = null!;
        public AsyncRelayCommand BackupToUsbCommand { get; private set; }
            = null!;
        public AsyncRelayCommand RestoreBackupCommand { get; private set; }
            = null!;
        public RelayCommand OpenBackupFolderCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SavePriceCommand { get; private set; }
            = null!;
        public AsyncRelayCommand SaveThresholdCommand { get; private set; }
            = null!;
        public AsyncRelayCommand ToggleActiveCommand { get; private set; }
            = null!;
        public RelayCommand CancelFormCommand { get; private set; }
            = null!;

        private void InitializeCommands()
        {
            ToggleApiCommand = new AsyncRelayCommand(
                async _ => await ToggleApiAsync(),
                _ => CurrentSession.Instance.HasPermission("ManageApi"));

            ManualSyncCommand = new AsyncRelayCommand(
                async _ =>
                {
                    if (!CurrentSession.Instance.HasPermission("ManageSettings"))
                        return;
                    SyncStatus = "جاري المزامنة...";
                    var success = await _cloudSyncService.SyncAsync();
                    SyncStatus = success
                        ? $"✅ تمت المزامنة: " +
                          $"{DateTime.Now:HH:mm}"
                        : "❌ فشلت المزامنة";
                },
                _ => CurrentSession.Instance.HasPermission("ManageSettings"));

            CreateBackupCommand = new AsyncRelayCommand(
                async _ => await CreateBackupAsync(),
                _ => CurrentSession.Instance.HasPermission("ManageBackup"));

            BackupToUsbCommand = new AsyncRelayCommand(
                async _ => await BackupToUsbAsync(),
                _ => CurrentSession.Instance.HasPermission("ManageBackup"));

            RestoreBackupCommand = new AsyncRelayCommand(
                async _ => await RestoreBackupAsync(),
                _ => SelectedBackup != null &&
                     CurrentSession.Instance.HasPermission("ManageBackup"));

            OpenBackupFolderCommand = new RelayCommand(_ =>
            {
                if (!CurrentSession.Instance.HasPermission("ManageBackup"))
                    return;
                var folder = System.IO.Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments),
                    "BlockFactory_Backups");

                System.Diagnostics.Process.Start(
                    "explorer.exe", folder);
            },
            _ => CurrentSession.Instance.HasPermission("ManageBackup"));

            SavePriceCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedProduct == null) return;
                try
                {
                    IsLoading = true;
                    var product = await _productService.GetByIdAsync(SelectedProduct.Id);
                    if (product == null) { ShowError("المنتج غير موجود"); return; }
                    product.PriceMin = FormPriceMin;
                    product.PriceMax = FormPriceMax;
                    product.DefaultPrice = FormDefaultPrice;
                    // TODO: save via service
                    SelectedProduct.PriceMin = FormPriceMin;
                    SelectedProduct.PriceMax = FormPriceMax;
                    SelectedProduct.DefaultPrice = FormDefaultPrice;
                    ShowSuccess("✅ تم حفظ الأسعار");
                    await LoadProductsAsync();
                }
                catch (Exception ex) { ShowError(ex.Message); }
                finally { IsLoading = false; }
            });

            SaveThresholdCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedProduct == null) return;
                try
                {
                    IsLoading = true;
                    SelectedProduct.MinThreshold = FormMinThreshold;
                    ShowSuccess("✅ تم حفظ الحد الأدنى");
                    await LoadProductsAsync();
                }
                catch (Exception ex) { ShowError(ex.Message); }
                finally { IsLoading = false; }
            });

            ToggleActiveCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedProduct == null) return;
                try
                {
                    IsLoading = true;
                    SelectedProduct.IsActive = !SelectedProduct.IsActive;
                    ShowSuccess(SelectedProduct.IsActive ? "✅ تم تفعيل المنتج" : "⛔ تم إيقاف المنتج");
                    await LoadProductsAsync();
                }
                catch (Exception ex) { ShowError(ex.Message); }
                finally { IsLoading = false; }
            });

            CancelFormCommand = new RelayCommand(_ =>
            {
                IsFormVisible = false;
                _selectedProduct = null;
                OnPropertyChanged(nameof(SelectedProduct));
                ClearMessages();
            });
        }

        // ─── API Toggle ──────────────────────────────
        private async Task ToggleApiAsync()
        {
            try
            {
                IsLoading = true;

                if (IsApiRunning)
                {
                    await _apiHostService.StopAsync();
                    IsApiRunning = false;
                    ApiUrl = string.Empty;
                    ShowSuccess("تم إيقاف الـ API");
                }
                else
                {
                    await _apiHostService.StartAsync();
                    IsApiRunning = true;
                    ApiUrl = _apiHostService.ApiUrl;
                    ShowSuccess(
                        $"الـ API يعمل على: {ApiUrl}");
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

        // ─── Backup Operations ───────────────────────
        private async Task CreateBackupAsync()
        {
            try
            {
                IsLoading = true;
                BackupStatus = "⏳ جاري إنشاء النسخة الاحتياطية...";

                var result = await _backupService.CreateBackupAsync();

                if (result.Success)
                {
                    BackupStatus =
                        $"✅ تم إنشاء النسخة: {result.SizeText}";
                    LoadBackupList();
                }
                else
                {
                    BackupStatus = $"❌ فشل: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                BackupStatus = $"❌ خطأ: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task BackupToUsbAsync()
        {
            try
            {
                IsLoading = true;
                BackupStatus = "⏳ جاري النسخ على USB...";

                var result = await _backupService.BackupToUsbAsync();

                BackupStatus = result.Success
                    ? $"✅ تم النسخ على USB: {result.SizeText}"
                    : $"❌ {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                BackupStatus = $"❌ خطأ: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RestoreBackupAsync()
        {
            if (SelectedBackup == null) return;

            var confirm = MessageBox.Show(
                $"⚠️ تحذير: سيتم استعادة النسخة الاحتياطية\n" +
                $"وسيتم فقدان جميع التغييرات الحالية!\n\n" +
                $"الملف: {SelectedBackup.FileName}\n" +
                $"التاريخ: {SelectedBackup.DateText}\n\n" +
                $"هل أنت متأكد؟",
                "تأكيد الاستعادة",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;
                BackupStatus = "⏳ جاري الاستعادة...";

                var result = await _backupService
                    .RestoreBackupAsync(SelectedBackup.FullPath);

                if (result.Success)
                {
                    BackupStatus = "✅ تمت الاستعادة بنجاح";
                    MessageBox.Show(
                        "تمت استعادة قاعدة البيانات بنجاح.\n" +
                        "يرجى إعادة تشغيل التطبيق.",
                        "تمت الاستعادة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information,
                        MessageBoxResult.OK,
                        MessageBoxOptions.RightAlign |
                        MessageBoxOptions.RtlReading);
                }
                else
                {
                    BackupStatus = $"❌ {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                BackupStatus = $"❌ خطأ: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadProductsAsync()
        {
            try
            {
                var list = await _productService.GetActiveProductsAsync();
                Products.Clear();
                foreach (var p in list.OrderBy(x => x.Name))
                    Products.Add(new ProductSettingsRow
                    {
                        Id = p.Id,
                        Name = p.Name,
                        TypeName = p.ProductType?.Name ?? "-",
                        Size = p.Size,
                        PriceMin = p.PriceMin,
                        PriceMax = p.PriceMax,
                        DefaultPrice = p.DefaultPrice,
                       // MinThreshold = p.MinStock,
                        IsActive = p.IsActive,
                      
                    });
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل المنتجات: {ex.Message}");
            }
        }

        private void LoadBackupList()
        {
            BackupList.Clear();
            foreach (var backup in _backupService.GetBackupList())
                BackupList.Add(backup);
        }
    }

    // ─── ProductSettingsRow ──────────────────────────
    public class ProductSettingsRow : BlockFactory.Desktop.ViewModels.Base.BaseViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public int Size { get; set; }
        public int StockQuantity { get; set; }
        private decimal _priceMin;
        public decimal PriceMin
        {
            get => _priceMin;
            set => SetProperty(ref _priceMin, value);
        }

        private decimal _priceMax;
        public decimal PriceMax
        {
            get => _priceMax;
            set => SetProperty(ref _priceMax, value);
        }

        private decimal _defaultPrice;
        public decimal DefaultPrice
        {
            get => _defaultPrice;
            set => SetProperty(ref _defaultPrice, value);
        }

        public int MinThreshold { get; set; }
        public int MinStock { get; set; }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string PriceRangeText => $"{PriceMin:N0} — {PriceMax:N0} ر.ي";
    }
}