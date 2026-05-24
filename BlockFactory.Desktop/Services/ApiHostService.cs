using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Services;
using BlockFactory.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace BlockFactory.Desktop.Services
{
    public class ApiHostService
    {
        private WebApplication? _app;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        public bool IsRunning => _isRunning;
        public string ApiUrl { get; private set; } = string.Empty;

        public async Task StartAsync()
        {
            if (_isRunning) return;

            try
            {
                _cts = new CancellationTokenSource();

                // ── 4. فتح Windows Firewall تلقائياً ──
                await OpenFirewallPortAsync(5050);

                var builder = WebApplication.CreateBuilder();

                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(
                        DatabaseConfig.GetConnectionString(),
                        sql => sql.MigrationsAssembly(
                            typeof(AppDbContext).Assembly.GetName().Name!)),
                    ServiceLifetime.Scoped);

                builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
                builder.Services.AddScoped<IAuthService, AuthService>();
                builder.Services.AddScoped<IDashboardService, DashboardService>();
                builder.Services.AddScoped<IReportService, ReportService>();
                builder.Services.AddScoped<IOrderService, OrderService>();
                builder.Services.AddScoped<ICustomerService, CustomerService>();
                builder.Services.AddScoped<IInventoryService, InventoryService>();
                builder.Services.AddScoped<IFinanceService, FinanceService>();
                builder.Services.AddScoped<IHRService, HRService>();
                builder.Services.AddScoped<IProductionService, ProductionService>();

                builder.Services.AddCors(opt =>
                    opt.AddPolicy("All", p =>
                        p.AllowAnyOrigin()
                         .AllowAnyMethod()
                         .AllowAnyHeader()));

                builder.WebHost.UseUrls("http://0.0.0.0:5050");

                _app = builder.Build();
                _app.UseCors("All");

                _app.MapGet("/", () => new
                {
                    Status = "Online",
                    Time = DateTime.Now.ToString("HH:mm:ss")
                });

                _app.MapGet("/api/dashboard",
                    async (IDashboardService svc)
                        => Results.Ok(new
                        {
                            Stats = await svc.GetStatsAsync(),
                            Alerts = await svc.GetAlertsAsync(),
                            RecentOrders = await svc.GetRecentOrdersAsync(5),
                            GeneratedAt = DateTime.Now
                        }));

                _app.MapGet("/api/sales/today",
                    async (IOrderService svc) =>
                    {
                        var orders = await svc.GetOrdersByDateAsync(
                            DateTime.Today, DateTime.Today);
                        return Results.Ok(new
                        {
                            TotalSales = orders.Sum(o => o.TotalAmount),
                            TotalCollected = orders.Sum(o => o.PaidAmount),
                            Count = orders.Count(),
                            Orders = orders
                        });
                    });

                _app.MapGet("/api/inventory",
                    async (IInventoryService svc)
                        => Results.Ok(await svc.GetSummaryAsync()));

                _app.MapGet("/api/finance/summary",
                    async (IFinanceService svc) =>
                    {
                        var monthStart = new DateTime(
                            DateTime.Today.Year,
                            DateTime.Today.Month, 1);
                        return Results.Ok(
                            await svc.GetFinancialSummaryAsync(
                                monthStart, DateTime.Today));
                    });

                _app.MapGet("/api/production/today",
                    async (IProductionService svc)
                        => Results.Ok(
                            await svc.GetDailySummaryAsync(DateTime.Today)));

                _app.MapGet("/api/customers/debt",
                    async (ICustomerService svc) =>
                    {
                        var customers = await svc.GetCustomersWithDebtAsync();
                        return Results.Ok(new
                        {
                            TotalDebt = customers.Sum(c => c.TotalDebt),
                            Count = customers.Count(),
                            Customers = customers
                        });
                    });

                // ── 3. استخدام Socket للحصول على IP الدقيق ──
                ApiUrl = $"http://{GetLocalIpAddress()}:5050";

                _ = Task.Run(async () =>
                {
                    await _app.RunAsync(_cts.Token);
                });

                _isRunning = true;
            }
            catch (Exception ex)
            {
                _isRunning = false;
                throw new Exception($"فشل تشغيل API: {ex.Message}");
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            _cts?.Cancel();
            if (_app != null)
                await _app.StopAsync();

            _isRunning = false;
            ApiUrl = string.Empty;
        }

        // ── 3. GetLocalIpAddress المحسّن باستخدام Socket ──
        private static string GetLocalIpAddress()
        {
            try
            {
                using var socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "localhost";
            }
            catch
            {
                return "localhost";
            }
        }

        // ── 4. فتح منفذ Windows Firewall تلقائياً ──
        private static async Task OpenFirewallPortAsync(int port)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"advfirewall firewall add rule " +
                                    $"name=\"BlockFactory API\" " +
                                    $"dir=in action=allow " +
                                    $"protocol=TCP localport={port}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
            }
            catch
            {
                // إذا فشل فتح الجدار الناري، نكمل التشغيل بدونه
            }
        }
    }
}
/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Services;
using BlockFactory.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlockFactory.Desktop.Services
{
    /// <summary>
    /// يشغّل الـ API داخل نفس الـ Process
    /// المدير يتصل عبر WiFi من هاتفه
    /// </summary>
    public class ApiHostService
    {
        private WebApplication? _app;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        public bool IsRunning => _isRunning;
        public string ApiUrl { get; private set; } = string.Empty;

        public Task StartAsync()
        {
            if (_isRunning) return Task.CompletedTask;

            try
            {
                _cts = new CancellationTokenSource();

                var builder = WebApplication.CreateBuilder();

                // إضافة نفس الـ Services
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(
                        DatabaseConfig.GetConnectionString(),
                        sql => sql.MigrationsAssembly(
                            typeof(AppDbContext).Assembly.GetName().Name!)),
                    ServiceLifetime.Scoped);

                builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

                builder.Services.AddScoped<IAuthService, AuthService>();
                builder.Services.AddScoped<IDashboardService, DashboardService>();
                builder.Services.AddScoped<IReportService, ReportService>();
                builder.Services.AddScoped<IOrderService, OrderService>();

                builder.Services.AddScoped<ICustomerService, CustomerService>();

                builder.Services.AddScoped<IInventoryService, InventoryService>();

                builder.Services.AddScoped<IFinanceService, FinanceService>();

                builder.Services.AddScoped<IHRService, HRService>();

                builder.Services.AddScoped<IProductionService, ProductionService>();

                builder.Services.AddCors(opt =>
                    opt.AddPolicy("All", p =>
                        p.AllowAnyOrigin()
                         .AllowAnyMethod()
                         .AllowAnyHeader()));

                builder.WebHost.UseUrls("http://0.0.0.0:5050");

                _app = builder.Build();
                _app.UseCors("All");

                // ─── نفس الـ Routes ───────────────

                _app.MapGet("/", () => new
                {
                    Status = "Online",
                    Time = DateTime.Now.ToString("HH:mm:ss")
                });

                _app.MapGet("/api/dashboard",
                    async (IDashboardService svc)
                        => Results.Ok(new
                        {
                            Stats = await svc.GetStatsAsync(),
                            Alerts = await svc.GetAlertsAsync(),
                            RecentOrders =
                                await svc.GetRecentOrdersAsync(5),
                            GeneratedAt = DateTime.Now
                        }));

                _app.MapGet("/api/sales/today",
                    async (IOrderService svc)
                    =>
                    {
                        var orders = await svc.GetOrdersByDateAsync(
                            DateTime.Today, DateTime.Today);
                        return Results.Ok(new
                        {
                            TotalSales = orders.Sum(o => o.TotalAmount),
                            TotalCollected = orders.Sum(o => o.PaidAmount),
                            Count = orders.Count(),
                            Orders = orders
                        });
                    });

                _app.MapGet("/api/inventory",
                    async (IInventoryService svc)
                        => Results.Ok(await svc.GetSummaryAsync()));

                _app.MapGet("/api/finance/summary",
                    async (IFinanceService svc)
                    =>
                    {
                        var monthStart = new DateTime(
                            DateTime.Today.Year,
                            DateTime.Today.Month, 1);
                        return Results.Ok(
                            await svc.GetFinancialSummaryAsync(
                                monthStart, DateTime.Today));
                    });

                _app.MapGet("/api/production/today",
                    async (IProductionService svc)
                        => Results.Ok(
                            await svc.GetDailySummaryAsync(DateTime.Today)));

                _app.MapGet("/api/customers/debt",
                    async (ICustomerService svc)
                    =>
                    {
                        var customers =
                            await svc.GetCustomersWithDebtAsync();
                        return Results.Ok(new
                        {
                            TotalDebt = customers.Sum(c => c.TotalDebt),
                            Count = customers.Count(),
                            Customers = customers
                        });
                    });

                // تحديد الـ IP المحلي للعرض
                ApiUrl = $"http://{GetLocalIpAddress()}:5050";

                _ = Task.Run(async () =>
                {
                    await _app.RunAsync(_cts.Token);
                });

                _isRunning = true;
            }
            catch (Exception ex)
            {
                _isRunning = false;
                throw new Exception($"فشل تشغيل API: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            _cts?.Cancel();
            if (_app != null)
                await _app.StopAsync();

            _isRunning = false;
            ApiUrl = string.Empty;
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(
                    System.Net.Dns.GetHostName());

                var ip = host.AddressList
                    .FirstOrDefault(a =>
                        a.AddressFamily ==
                        System.Net.Sockets.AddressFamily.InterNetwork);

                return ip?.ToString() ?? "localhost";
            }
            catch
            {
                return "localhost";
            }
        }
    }
}*/
