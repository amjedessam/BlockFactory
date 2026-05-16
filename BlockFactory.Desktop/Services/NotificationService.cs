using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace BlockFactory.Desktop.Services
{
    public class NotificationService
    {
        private static NotificationService? _instance;
        public static NotificationService Instance =>
            _instance ??= new NotificationService();

        public ObservableCollection<AppNotification> Notifications { get; }
            = new();

        public event Action<AppNotification>? OnNotification;

        private NotificationService() { }

        // ─── إشعار نجاح ─────────────────────────────
        public void Success(string message, string? title = null)
        {
            Show(new AppNotification
            {
                Title = title ?? "تم بنجاح",
                Message = message,
                Type = NotificationType.Success,
                Icon = "✅",
                BackgroundColor = "#EAFAF1",
                ForegroundColor = "#27AE60"
            });
        }

        // ─── إشعار خطأ ──────────────────────────────
        public void Error(string message, string? title = null)
        {
            Show(new AppNotification
            {
                Title = title ?? "خطأ",
                Message = message,
                Type = NotificationType.Error,
                Icon = "❌",
                BackgroundColor = "#FDEDEC",
                ForegroundColor = "#E74C3C"
            });
        }

        // ─── إشعار تحذير ────────────────────────────
        public void Warning(string message, string? title = null)
        {
            Show(new AppNotification
            {
                Title = title ?? "تنبيه",
                Message = message,
                Type = NotificationType.Warning,
                Icon = "⚠️",
                BackgroundColor = "#FEF9E7",
                ForegroundColor = "#F39C12"
            });
        }

        // ─── إشعار معلومات ──────────────────────────
        public void Info(string message, string? title = null)
        {
            Show(new AppNotification
            {
                Title = title ?? "معلومة",
                Message = message,
                Type = NotificationType.Info,
                Icon = "ℹ️",
                BackgroundColor = "#EBF5FB",
                ForegroundColor = "#2980B9"
            });
        }

        private void Show(AppNotification notification)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Notifications.Insert(0, notification);
                OnNotification?.Invoke(notification);

                // حذف الإشعار بعد 4 ثواني
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(4)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    Notifications.Remove(notification);
                };
                timer.Start();
            });
        }
    }

    public class AppNotification
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public string ForegroundColor { get; set; } = "#2C3E50";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }
}
