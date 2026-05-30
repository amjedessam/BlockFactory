using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Session;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.Services;
using BlockFactory.Desktop.ViewModels.Base;

namespace BlockFactory.Desktop.ViewModels.Settings
{
    public class UserListRow
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class UsersViewModel : BaseViewModel
    {
        private const string AccountantRoleName = "Accountant";

        private readonly IAuthService _auth;
        private readonly ApiHostService _apiHostService;

        private int? _accountantRoleId;

        private bool _isAccountantFormVisible;
        public bool IsAccountantFormVisible
        {
            get => _isAccountantFormVisible;
            set => SetProperty(ref _isAccountantFormVisible, value);
        }

        public bool CanManageApi => CurrentSession.Instance.HasPermission("ManageApi");

        private bool _isApiRunning;
        public bool IsApiRunning
        {
            get => _isApiRunning;
            set
            {
                SetProperty(ref _isApiRunning, value);
                OnPropertyChanged(nameof(ApiStatusText));
                OnPropertyChanged(nameof(ApiToggleText));
            }
        }

        private string _apiUrl = string.Empty;
        public string ApiUrl
        {
            get => _apiUrl;
            set => SetProperty(ref _apiUrl, value);
        }

        public string ApiStatusText => IsApiRunning ? "🟢 يعمل" : "🔴 متوقف";
        public string ApiToggleText => IsApiRunning ? "⛔ إيقاف API" : "▶️ تشغيل API";

        public AsyncRelayCommand ToggleApiCommand { get; private set; }

        private string _accountantFullName = string.Empty;
        public string AccountantFullName
        {
            get => _accountantFullName;
            set
            {
                if (SetProperty(ref _accountantFullName, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _accountantUsername = string.Empty;
        public string AccountantUsername
        {
            get => _accountantUsername;
            set
            {
                if (SetProperty(ref _accountantUsername, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public RelayCommand ShowAddAccountantFormCommand { get; }
        public RelayCommand CancelAddAccountantFormCommand { get; }
        public AsyncRelayCommand SaveAccountantCommand { get; }
        public AsyncRelayCommand LoadCommand { get; }

        public UsersViewModel(
            IAuthService auth,
            ApiHostService apiHostService)
        {
            _auth = auth;
            _apiHostService = apiHostService;

            LoadCommand = new AsyncRelayCommand(
                async _ => await LoadAsync(),
                _ => CurrentSession.Instance.HasPermission("ManageUsers"));

            ShowAddAccountantFormCommand = new RelayCommand(
                _ =>
                {
                    ClearMessages();
                    if (!CurrentSession.Instance.HasPermission("ManageUsers"))
                        return;
                    if (!_accountantRoleId.HasValue)
                    {
                        ShowError(
                            "دور «محاسب» غير موجود في قاعدة البيانات. " +
                            "أعد تشغيل التطبيق بعد التأكد من تهيئة الأدوار.");
                        return;
                    }

                    AccountantFullName = string.Empty;
                    AccountantUsername = string.Empty;
                    IsAccountantFormVisible = true;
                },
                _ => CurrentSession.Instance.HasPermission("ManageUsers"));

            CancelAddAccountantFormCommand = new RelayCommand(
                _ =>
                {
                    IsAccountantFormVisible = false;
                    ClearAccountantForm();
                    ClearMessages();
                });

            SaveAccountantCommand = new AsyncRelayCommand(
                SaveAccountantAsync,
                CanSaveAccountant);

            ToggleApiCommand = new AsyncRelayCommand(
                async _ => await ToggleApiAsync(),
                _ => CurrentSession.Instance.HasPermission("ManageApi"));
        }

        public ObservableCollection<UserListRow> Users { get; } = new();

        private void ClearAccountantForm()
        {
            AccountantFullName = string.Empty;
            AccountantUsername = string.Empty;
        }

        private bool CanSaveAccountant(object? parameter)
        {
            if (!CurrentSession.Instance.HasPermission("ManageUsers"))
                return false;
            if (!_accountantRoleId.HasValue || !IsAccountantFormVisible)
                return false;
            if (IsLoading)
                return false;

            var pwd = parameter is PasswordBox pb
                ? pb.Password
                : string.Empty;

            return !string.IsNullOrWhiteSpace(AccountantFullName.Trim()) &&
                   !string.IsNullOrWhiteSpace(AccountantUsername.Trim()) &&
                   pwd.Length >= 6;
        }

        private async Task SaveAccountantAsync(object? parameter)
        {
            if (!CanSaveAccountant(parameter))
                return;

            var password = parameter is PasswordBox pb
                ? pb.Password
                : string.Empty;

            try
            {
                IsLoading = true;
                ClearMessages();
                CommandManager.InvalidateRequerySuggested();

                if (!_accountantRoleId.HasValue)
                {
                    ShowError("تعذر تحديد دور المحاسب.");
                    return;
                }

                var result = await _auth.CreateUserAsync(
                    AccountantFullName.Trim(),
                    AccountantUsername.Trim(),
                    password,
                    _accountantRoleId.Value);

                if (result.Success)
                {
                    if (parameter is PasswordBox box)
                        box.Password = string.Empty;

                    ShowSuccess("تم إنشاء حساب المحاسب. يمكنه تسجيل الدخول بالبيانات التي أدخلتها.");
                    IsAccountantFormVisible = false;
                    ClearAccountantForm();
                    await LoadUsersGridAsync();
                }
                else
                    ShowError(result.Message);
            }
            catch (System.Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public async Task LoadAsync()
        {
            if (!CurrentSession.Instance.HasPermission("ManageUsers"))
                return;

            try
            {
                IsLoading = true;
                ClearMessages();
                _accountantRoleId =
                    await _auth.GetRoleIdByNameAsync(AccountantRoleName);
                IsApiRunning = _apiHostService.IsRunning;
                ApiUrl = _apiHostService.ApiUrl;
                await LoadUsersGridAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadUsersGridAsync()
        {
            var list = await _auth.GetAllUsersAsync();
            Users.Clear();
            foreach (var u in list.OrderBy(x => x.FullName))
            {
                Users.Add(new UserListRow
                {
                    FullName = u.FullName,
                    Username = u.Username,
                    RoleName = u.Role?.Name ?? "-",
                    IsActive = u.IsActive
                });
            }
        }

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
                    ShowSuccess($"الـ API يعمل على: {ApiUrl}");
                }
            }
            catch (System.Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
