using BlockFactory.Core.Session;

namespace BlockFactory.Desktop.Services
{
    /// <summary>
    /// يربط اسم شاشة التنقل بأقل صلاحية مطلوبة.
    /// </summary>
    internal static class ViewAccess
    {
        public static bool CanNavigate(string viewName)
        {
            var s = CurrentSession.Instance;
            if (!s.IsLoggedIn)
                return false;

            return viewName switch
            {
                "Dashboard" => s.HasPermission("ViewDashboard"),
                "Orders" or "NewOrder" => s.HasPermission("ManageSales"),
                "Customers" or "Pledges" => s.HasPermission("ManageCustomers"),
                "Production" => s.HasPermission("ManageProduction"),
                "Inventory" => s.HasPermission("ViewInventory"),
                "Suppliers" => s.HasPermission("ManageSuppliers"),
                "Workers" => s.HasPermission("ViewHR"),
                "Salaries" => s.HasPermission("ViewSalaries"),
                "Finance" => s.HasPermission("ManageFinance"),
                "Reports" => s.HasPermission("ViewReports"),
                "Products" => s.HasPermission("ChangePrice"),
                "Users" => s.HasPermission("ManageUsers"),
                "Settings" => s.HasPermission("ManageSettings"),
                _ => true
            };
        }
    }
}
