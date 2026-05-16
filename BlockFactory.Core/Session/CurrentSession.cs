using System;

using BlockFactory.Core.Models.Auth;

namespace BlockFactory.Core.Session
{
    /// <summary>
    /// يحتفظ ببيانات المستخدم المسجّل دخوله طوال فترة التشغيل
    /// Singleton — instance واحدة فقط في التطبيق
    /// </summary>
    public sealed class CurrentSession
    {
        private static readonly Lazy<CurrentSession> _instance =
            new(() => new CurrentSession());

        public static CurrentSession Instance => _instance.Value;

        private CurrentSession() { }

        public int UserId { get; private set; }
        public string FullName { get; private set; } = string.Empty;
        public string Username { get; private set; } = string.Empty;
        public string RoleName { get; private set; } = string.Empty;
        public DateTime LoginTime { get; private set; }
        public bool IsLoggedIn { get; private set; }

        public bool IsAdmin => RoleName == "Admin";
        public bool IsAccountant => RoleName == "Accountant";

        public void SetSession(User user)
        {
            UserId = user.Id;
            FullName = user.FullName;
            Username = user.Username;
            RoleName = user.Role?.Name ?? string.Empty;
            LoginTime = DateTime.Now;
            IsLoggedIn = true;
        }

        public void ClearSession()
        {
            UserId = 0;
            FullName = string.Empty;
            Username = string.Empty;
            RoleName = string.Empty;
            IsLoggedIn = false;
        }

        /// <summary>
        /// فحص صلاحية وظيفة محددة حسب الدور (مدير / محاسب).
        /// </summary>
        public bool HasPermission(string permission)
        {
            return permission switch
            {
                // ─── Admin فقط ──────────────────────────
                "ManageUsers" => IsAdmin,
                "ManageSettings" => IsAdmin,
                "DeleteRecords" => IsAdmin,
                "ManageHR" => IsAdmin,
                "ManageSalaries" => IsAdmin,
                "AdjustStock" => IsAdmin,
                "ChangePrice" => IsAdmin,
                "ManageApi" => IsAdmin,
                "ManageBackup" => IsAdmin,
                "DeleteExpense" => IsAdmin,
                "DeleteOrder" => IsAdmin,
                "DeleteProduction" => IsAdmin,
                "DeleteCustomer" => IsAdmin,

                // ─── Admin + Accountant ─────────────────
                "ViewDashboard" => IsAdmin || IsAccountant,
                "ManageSales" => IsAdmin || IsAccountant,
                "ManageCustomers" => IsAdmin || IsAccountant,
                "ManageProduction" => IsAdmin || IsAccountant,
                "ViewInventory" => IsAdmin || IsAccountant,
                "ManageSuppliers" => IsAdmin || IsAccountant,
                "PaySupplier" => IsAdmin || IsAccountant,
                "ManageFinance" => IsAdmin || IsAccountant,
                "AddExpense" => IsAdmin || IsAccountant,
                "ViewReports" => IsAdmin || IsAccountant,
                "PrintReports" => IsAdmin || IsAccountant,
                "AddAdvance" => IsAdmin || IsAccountant,
                "ViewSalaries" => IsAdmin || IsAccountant,
                "ViewHR" => IsAdmin || IsAccountant,

                _ => IsAdmin
            };
        }
    }
}
