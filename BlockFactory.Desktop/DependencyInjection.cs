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
using BlockFactory.Desktop.ViewModels.Orders;
using BlockFactory.Desktop.Views.Customers;
using BlockFactory.Desktop.Views.Dashboard;
using BlockFactory.Desktop.Views.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlockFactory.Desktop
{
    public static class DependencyInjection
    {
        public static IServiceProvider Configure()
        {
            var services = new ServiceCollection();

            // ─── Database ──────────────────────────
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    DatabaseConfig.GetConnectionString()),
                ServiceLifetime.Scoped);

            // ─── Unit of Work ──────────────────────
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ─── Services ──────────────────────────
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddSingleton<BackupService>();
            services.AddScoped<IReportService, ReportService>();
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

            // ─── return يجب أن يكون هنا فقط ────────
            return services.BuildServiceProvider();
        }
    }
}