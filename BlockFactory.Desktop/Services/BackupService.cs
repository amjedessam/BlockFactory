using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Windows.Threading;

namespace BlockFactory.Desktop.Services
{
    public class BackupService
    {
        private readonly DispatcherTimer _timer;
        private bool _isRunning;

        // مجلدات النسخ الاحتياطية
        private static readonly string BackupFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "BlockFactory_Backups");

        public BackupService()
        {
            // النسخ الاحتياطي كل 4 ساعات
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(4)
            };
            _timer.Tick += async (s, e) => await CreateBackupAsync();
        }

        public void Start()
        {
            if (_isRunning) return;

            Directory.CreateDirectory(BackupFolder);
            _timer.Start();
            _isRunning = true;

            // نسخة احتياطية فور التشغيل
            _ = Task.Run(async () => await CreateBackupAsync());
        }

        public void Stop()
        {
            _timer.Stop();
            _isRunning = false;
        }

        // ─── إنشاء نسخة احتياطية ────────────────────
        public async Task<BackupResult> CreateBackupAsync(
            string? customPath = null)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupName = $"BlockFactory_{timestamp}.bak";
                var backupPath = customPath
                    ?? Path.Combine(BackupFolder, backupName);

                // نسخ قاعدة البيانات
                await BackupDatabaseAsync(backupPath);

                // حذف النسخ القديمة (الاحتفاظ بآخر 10 نسخ)
                CleanOldBackups();

                return new BackupResult
                {
                    Success = true,
                    BackupPath = backupPath,
                    BackupSize = new FileInfo(backupPath).Length,
                    CreatedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new BackupResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ─── نسخ احتياطي لـ USB ──────────────────────
        public async Task<BackupResult> BackupToUsbAsync()
        {
            try
            {
                // البحث عن USB متصل
                var usbDrive = DriveInfo.GetDrives()
                    .FirstOrDefault(d =>
                        d.DriveType == DriveType.Removable &&
                        d.IsReady);

                if (usbDrive == null)
                    return new BackupResult
                    {
                        Success = false,
                        ErrorMessage = "لم يتم العثور على USB"
                    };

                var usbBackupFolder = Path.Combine(
                    usbDrive.RootDirectory.FullName,
                    "BlockFactory_Backups");

                Directory.CreateDirectory(usbBackupFolder);

                var timestamp = DateTime.Now
                    .ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(
                    usbBackupFolder,
                    $"BlockFactory_{timestamp}.bak");

                return await CreateBackupAsync(backupPath);
            }
            catch (Exception ex)
            {
                return new BackupResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ─── استعادة نسخة احتياطية ──────────────────
        public async Task<BackupResult> RestoreBackupAsync(
            string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                    return new BackupResult
                    {
                        Success = false,
                        ErrorMessage = "ملف النسخة الاحتياطية غير موجود"
                    };

                // إنشاء نسخة احتياطية قبل الاستعادة
                await CreateBackupAsync();

                // استعادة قاعدة البيانات
                await RestoreDatabaseAsync(backupPath);

                return new BackupResult
                {
                    Success = true,
                    BackupPath = backupPath,
                    CreatedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new BackupResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ─── قائمة النسخ الاحتياطية ─────────────────
        public IEnumerable<BackupInfo> GetBackupList()
        {
            if (!Directory.Exists(BackupFolder))
                return Enumerable.Empty<BackupInfo>();

            return Directory.GetFiles(BackupFolder, "*.bak")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new BackupInfo
                {
                    FileName = f.Name,
                    FullPath = f.FullName,
                    Size = f.Length,
                    CreatedAt = f.CreationTime
                });
        }

        // ─── Private Methods ─────────────────────────

        private async Task BackupDatabaseAsync(string backupPath)
        {
            // نسخ ملف MDF مباشرة (لـ LocalDB)
            // أو استخدام SQL BACKUP command

            var connectionString = Data.DatabaseConfig
                .GetConnectionString();

            using var connection =
                new Microsoft.Data.SqlClient.SqlConnection(
                    connectionString);

            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText =
                $"BACKUP DATABASE [BlockFactoryDB] " +
                $"TO DISK = '{backupPath}' " +
                $"WITH FORMAT, INIT, " +
                $"NAME = 'BlockFactory Full Backup', " +
                $"COMPRESSION";
            command.CommandTimeout = 300; // 5 دقائق

            await command.ExecuteNonQueryAsync();
        }

        private async Task RestoreDatabaseAsync(string backupPath)
        {
            var connectionString = Data.DatabaseConfig
                .GetConnectionString()
                .Replace("BlockFactoryDB", "master");

            using var connection =
                new Microsoft.Data.SqlClient.SqlConnection(
                    connectionString);

            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText =
                $"USE master; " +
                $"ALTER DATABASE [BlockFactoryDB] " +
                $"SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"RESTORE DATABASE [BlockFactoryDB] " +
                $"FROM DISK = '{backupPath}' " +
                $"WITH REPLACE; " +
                $"ALTER DATABASE [BlockFactoryDB] " +
                $"SET MULTI_USER;";
            command.CommandTimeout = 600;

            await command.ExecuteNonQueryAsync();
        }

        private void CleanOldBackups(int keepCount = 10)
        {
            try
            {
                var files = Directory
                    .GetFiles(BackupFolder, "*.bak")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(keepCount)
                    .ToList();

                foreach (var file in files)
                {
                    try { file.Delete(); }
                    catch { }
                }
            }
            catch { }
        }
    }

    public class BackupResult
    {
        public bool Success { get; set; }
        public string? BackupPath { get; set; }
        public long BackupSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }

        public string SizeText => BackupSize > 0
            ? $"{BackupSize / 1024.0 / 1024.0:F1} MB"
            : "-";
    }

    public class BackupInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SizeText => $"{Size / 1024.0 / 1024.0:F1} MB";
        public string DateText =>
            CreatedAt.ToString("dd/MM/yyyy HH:mm");
    }
}
