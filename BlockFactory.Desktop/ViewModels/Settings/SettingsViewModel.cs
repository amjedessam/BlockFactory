using System;
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
}
