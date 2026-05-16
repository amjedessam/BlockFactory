// BlockFactory.Desktop/DependencyInjection.cs

using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Services;
using BlockFactory.Data;
using BlockFactory.Desktop.Services;
using BlockFactory.Desktop.ViewModels;
using BlockFactory.Desktop.ViewModels.Auth;
using BlockFactory.Desktop.ViewModels.Customers;
using BlockFactory.Desktop.ViewModels.Dashboard;
using BlockFactory.Desktop.ViewModels.Finance;
using BlockFactory.Desktop.ViewModels.HR;
using BlockFactory.Desktop.ViewModels.Inventory;
using BlockFactory.Desktop.ViewModels.Orders;
using BlockFactory.Desktop.ViewModels.Production;
using BlockFactory.Desktop.ViewModels.Reports;
using BlockFactory.Desktop.ViewModels.Settings;
using BlockFactory.Desktop.ViewModels.Suppliers;
using BlockFactory.Desktop.Views.Customers;
using BlockFactory.Desktop.Views.Dashboard;
using BlockFactory.Desktop.Views.Finance;
using BlockFactory.Desktop.Views.HR;
using BlockFactory.Desktop.Views.Inventory;
using BlockFactory.Desktop.Views.Orders;
using BlockFactory.Desktop.Views.Production;
using BlockFactory.Desktop.Views.Reports;
using BlockFactory.Desktop.Views.Settings;
using BlockFactory.Desktop.Views.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlockFactory.Desktop
{
    public static class DependencyInjection
    {
        public static IServiceProvider Configure()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            // ─── Database ──────────────────────────
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    DatabaseConfig.GetConnectionString(),
                    sql => sql.MigrationsAssembly(
                        typeof(AppDbContext).Assembly.GetName().Name!)),
                ServiceLifetime.Scoped);

            // ─── Unit of Work ──────────────────────
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ─── Services ──────────────────────────
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IInventoryService, InventoryService>();

            // ─── Navigation ────────────────────────
            services.AddSingleton<NavigationService>();
            services.AddSingleton<INavigationService>(
                p => p.GetRequiredService<NavigationService>());

            // ─── ViewModels ────────────────────────
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainShellViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<OrdersViewModel>();
            services.AddTransient<NewOrderViewModel>();
            services.AddTransient<CustomersViewModel>();
            services.AddTransient<PledgesViewModel>();

            // ─── Views ─────────────────────────────
            services.AddTransient<DashboardView>();
            services.AddTransient<OrdersView>();
            services.AddTransient<NewOrderView>();
            services.AddTransient<CustomersView>();
            services.AddTransient<PledgesView>();

            services.AddScoped<IProductionService, ProductionService>();
            services.AddTransient<ProductionViewModel>();
            services.AddTransient<ProductionView>();

            services.AddScoped<IHRService, HRService>();
            services.AddTransient<WorkersViewModel>();
            services.AddTransient<SalariesViewModel>();
            services.AddTransient<WorkersView>();
            services.AddTransient<SalariesView>();


            services.AddScoped<IFinanceService, FinanceService>();
            services.AddTransient<FinanceViewModel>();
            services.AddTransient<FinanceView>();


            services.AddScoped<ISupplierService, SupplierService>();
            services.AddTransient<SuppliersViewModel>();
            services.AddTransient<SuppliersView>();



            services.AddScoped<IReportService, ReportService>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<ReportsView>();

            services.AddTransient<InventoryViewModel>();
            services.AddTransient<InventoryView>();

            services.AddTransient<UsersViewModel>();
            services.AddTransient<UsersView>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<ProductsView>();

            // Singleton Services
            services.AddSingleton<ApiHostService>();
            services.AddSingleton<BackupService>();
            services.AddSingleton<CloudSyncService>();

            // ViewModels + Views
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsView>();

            // ─── return يجب أن يكون هنا فقط ────────
            return services.BuildServiceProvider();
        }
    }
}