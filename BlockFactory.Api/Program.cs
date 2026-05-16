
using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Services;
using BlockFactory.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ───────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        DatabaseConfig.GetConnectionString(),
        sql => sql.MigrationsAssembly(
            typeof(AppDbContext).Assembly.GetName().Name!)));

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

// ─── CORS — للموبايل ────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseCors("MobileApp");

// ════════════════════════════════════════════════════
// API Endpoints
// ════════════════════════════════════════════════════

// ─── Health Check ────────────────────────────────────
app.MapGet("/", () => new
{
    Status = "Online",
    System = "Block Factory ERP",
    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
});

// ─── Dashboard ───────────────────────────────────────
app.MapGet("/api/dashboard", async (
    IDashboardService dashboardService) =>
{
    try
    {
        var stats = await dashboardService.GetStatsAsync();
        var alerts = await dashboardService.GetAlertsAsync();
        var orders = await dashboardService.GetRecentOrdersAsync(5);

        return Results.Ok(new
        {
            Stats = stats,
            Alerts = alerts,
            RecentOrders = orders,
            GeneratedAt = DateTime.Now
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// ─── المبيعات ────────────────────────────────────────
app.MapGet("/api/sales/today", async (
    IOrderService orderService) =>
{
    var from = DateTime.Today;
    var to = DateTime.Today;
    var orders = await orderService.GetOrdersByDateAsync(from, to);
    return Results.Ok(new
    {
        Date = DateTime.Today.ToString("dd/MM/yyyy"),
        Count = orders.Count(),
        TotalSales = orders.Sum(o => o.TotalAmount),
        TotalCollected = orders.Sum(o => o.PaidAmount),
        TotalRemaining = orders.Sum(o => o.RemainingAmount),
        Orders = orders
    });
});

app.MapGet("/api/sales/range", async (
    DateTime from,
    DateTime to,
    IOrderService orderService) =>
{
    var orders = await orderService.GetOrdersByDateAsync(from, to);
    return Results.Ok(new
    {
        From = from.ToString("dd/MM/yyyy"),
        To = to.ToString("dd/MM/yyyy"),
        Count = orders.Count(),
        TotalSales = orders.Sum(o => o.TotalAmount),
        TotalCollected = orders.Sum(o => o.PaidAmount),
        Orders = orders
    });
});

// ─── العملاء والديون ─────────────────────────────────
app.MapGet("/api/customers/debt", async (
    ICustomerService customerService) =>
{
    var customers = await customerService.GetCustomersWithDebtAsync();
    return Results.Ok(new
    {
        TotalDebt = customers.Sum(c => c.TotalDebt),
        Count = customers.Count(),
        Customers = customers.OrderByDescending(c => c.TotalDebt)
    });
});

app.MapGet("/api/customers/pledges", async (
    ICustomerService customerService) =>
{
    var pledges = await customerService.GetActivePledgesAsync();
    return Results.Ok(new
    {
        Count = pledges.Count(),
        OverduePledges = pledges.Count(p => p.IsOverdue),
        Pledges = pledges.OrderBy(p => p.DueDate)
    });
});

// ─── الإنتاج ─────────────────────────────────────────
app.MapGet("/api/production/today", async (
    IProductionService productionService) =>
{
    var summary = await productionService
        .GetDailySummaryAsync(DateTime.Today);
    return Results.Ok(summary);
});

app.MapGet("/api/production/stats", async (
    IProductionService productionService) =>
{
    var stats = await productionService.GetStatsAsync();
    return Results.Ok(stats);
});

// ─── المخزون ─────────────────────────────────────────
app.MapGet("/api/inventory", async (
    IInventoryService inventoryService) =>
{
    var summary = await inventoryService.GetSummaryAsync();
    return Results.Ok(summary);
});

app.MapGet("/api/inventory/low-stock", async (
    IInventoryService inventoryService) =>
{
    var lowStock = await inventoryService.GetLowStockProductsAsync();
    var lowMaterials = await inventoryService.GetLowRawMaterialsAsync();
    return Results.Ok(new
    {
        LowProducts = lowStock,
        LowMaterials = lowMaterials
    });
});

// ─── المالية ─────────────────────────────────────────
app.MapGet("/api/finance/summary", async (
    IFinanceService financeService) =>
{
    var monthStart = new DateTime(
        DateTime.Today.Year,
        DateTime.Today.Month, 1);

    var summary = await financeService
        .GetFinancialSummaryAsync(monthStart, DateTime.Today);

    return Results.Ok(summary);
});

app.MapGet("/api/finance/wallets", async (
    IFinanceService financeService) =>
{
    var wallets = await financeService.GetWalletsAsync();
    return Results.Ok(wallets);
});

// ─── الموارد البشرية ──────────────────────────────────
app.MapGet("/api/hr/workers", async (
    IHRService hrService) =>
{
    var workers = await hrService.GetAllWorkersAsync();
    return Results.Ok(new
    {
        Total = workers.Count(),
        Active = workers.Count(w => w.Status == "نشط"),
        TotalSalaries = workers.Sum(w => w.BasicSalary),
        Workers = workers
    });
});

app.MapGet("/api/hr/advances", async (
    IHRService hrService) =>
{
    var advances = await hrService.GetPendingAdvancesAsync();
    return Results.Ok(new
    {
        Count = advances.Count(),
        TotalAmount = advances.Sum(a => a.Amount),
        Advances = advances
    });
});

// ─── تشغيل السيرفر ───────────────────────────────────
// الاستماع على كل الـ IPs المتاحة
app.Run("http://0.0.0.0:5050");