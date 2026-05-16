
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Session;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Windows;
using System.Windows.Input;

namespace BlockFactory.Desktop.ViewModels.Auth
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            LoginCommand = new AsyncRelayCommand(
                ExecuteLoginAsync,
                _ => CanLogin()
            );
        }

        // ─── Properties ────────────────────────────

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                ClearMessages();
                CommandManager.InvalidateRequerySuggested(); // ← تحديث الزر
            }
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _appVersion = "الإصدار 1.0.0";
        public string AppVersion
        {
            get => _appVersion;
            set => SetProperty(ref _appVersion, value);
        }

        private string _currentDate = DateTime.Now
            .ToString("dddd، d MMMM yyyy",
            new System.Globalization.CultureInfo("ar-YE"));
        public string CurrentDate
        {
            get => _currentDate;
            set => SetProperty(ref _currentDate, value);
        }

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand LoginCommand { get; }

        // ─── Logic ──────────────────────────────────

        private bool CanLogin()
            => !string.IsNullOrWhiteSpace(Username) && !IsLoading; // ← حذف شرط Password

        private async Task ExecuteLoginAsync(object? parameter)
        {
            // استلام كلمة المرور من PasswordBox عبر parameter
            if (parameter is System.Windows.Controls.PasswordBox pb)
                Password = pb.Password;

            if (!CanLogin()) return;

            // التحقق من كلمة المرور بعد قراءتها
            if (string.IsNullOrWhiteSpace(Password))
            {
                ShowError("يرجى إدخال كلمة المرور");
                return;
            }

            try
            {
                IsLoading = true;
                ClearMessages();
                CommandManager.InvalidateRequerySuggested();

                var result = await _authService
                    .LoginAsync(Username.Trim(), Password);

                if (result.Success)
                {
                    ShowSuccess("جاري تحميل النظام...");
                    await Task.Delay(500);
                    OpenMainWindow();
                }
                else
                {
                    ShowError(result.Message);
                    Password = string.Empty;
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ غير متوقع: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void OpenMainWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = new MainShell();
                mainWindow.Show();

                foreach (Window window in Application.Current.Windows)
                {
                    if (window is LoginWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            });
        }
    }
}

/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Session;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Windows;

namespace BlockFactory.Desktop.ViewModels.Auth
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            LoginCommand = new AsyncRelayCommand(
                ExecuteLoginAsync,
                _ => CanLogin()
            );
        }

        // ─── Properties ────────────────────────────

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                ClearMessages();
             
            }
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                ClearMessages();
            }
        }

        private string _appVersion = "الإصدار 1.0.0";
        public string AppVersion
        {
            get => _appVersion;
            set => SetProperty(ref _appVersion, value);
        }

        private string _currentDate = DateTime.Now
            .ToString("dddd، d MMMM yyyy",
            new System.Globalization.CultureInfo("ar-YE"));
        public string CurrentDate
        {
            get => _currentDate;
            set => SetProperty(ref _currentDate, value);
        }

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand LoginCommand { get; }

        // ─── Logic ──────────────────────────────────

        private bool CanLogin()
            => !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !IsLoading;

        private async Task ExecuteLoginAsync(object? parameter)
        {
            // استلام كلمة المرور من PasswordBox عبر parameter
            if (parameter is System.Windows.Controls.PasswordBox pb)
                Password = pb.Password;

            if (!CanLogin()) return;

            try
            {
                IsLoading = true;
                ClearMessages();

                var result = await _authService
                    .LoginAsync(Username.Trim(), Password);

                if (result.Success)
                {
                    ShowSuccess("جاري تحميل النظام...");

                    // تأخير بسيط لإظهار رسالة النجاح
                    await Task.Delay(500);

                    // فتح النافذة الرئيسية
                    OpenMainWindow();
                }
                else
                {
                    ShowError(result.Message);
                    Password = string.Empty;
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ غير متوقع: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenMainWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = new MainShell();
                mainWindow.Show();

                // إغلاق نافذة تسجيل الدخول
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is LoginWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            });
        }
    }
}*/
