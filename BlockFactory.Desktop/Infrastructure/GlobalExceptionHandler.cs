using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BlockFactory.Desktop.Infrastructure
{
    public static class GlobalExceptionHandler
    {
        private static readonly string LogFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "BlockFactory", "Logs");

        public static void Register()
        {
            Application.Current.DispatcherUnhandledException +=
                OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException +=
                OnUnhandledException;
            TaskScheduler.UnobservedTaskException +=
                OnUnobservedTaskException;

            Directory.CreateDirectory(LogFolder);
        }

        private static void OnDispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            LogError(e.Exception);
            ShowError(e.Exception, fatal: false);
        }

        private static void OnUnhandledException(
            object sender,
            UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is not Exception ex)
                return;

            LogError(ex);
            if (e.IsTerminating)
                ShowError(ex, fatal: true);
        }

        private static void OnUnobservedTaskException(
            object? sender,
            UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            LogError(e.Exception);
        }

        private static void LogError(Exception ex)
        {
            try
            {
                var logFile = Path.Combine(
                    LogFolder,
                    $"error_{DateTime.Today:yyyyMMdd}.log");
                var entry =
                    $"\n{DateTime.Now:O}\n{ex}\n";
                File.AppendAllText(logFile, entry);
            }
            catch
            {
                // ignore logging failures
            }
        }

        private static void ShowError(Exception ex, bool fatal)
        {
            try
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    var msg = fatal
                        ? $"خطأ فادح — سيتم إغلاق التطبيق\n\n{ex.Message}"
                        : $"حدث خطأ غير متوقع\n\n{ex.Message}\n\n" +
                          "تم حفظ التفاصيل في سجل التطبيق.";
                    MessageBox.Show(
                        msg,
                        fatal ? "خطأ فادح" : "خطأ",
                        fatal
                            ? MessageBoxButton.OK
                            : MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        MessageBoxResult.OK,
                        MessageBoxOptions.RightAlign |
                        MessageBoxOptions.RtlReading);
                    if (fatal)
                        Application.Current.Shutdown(1);
                });
            }
            catch
            {
                // ignore
            }
        }
    }
}
