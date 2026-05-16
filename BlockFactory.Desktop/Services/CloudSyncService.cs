using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Desktop.Services
{
    public class CloudSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly DispatcherTimer _syncTimer;
        private bool _isSyncing;
        private DateTime _lastSyncTime;

        // Supabase Config
        private const string SupabaseUrl =
            "https://YOUR_PROJECT.supabase.co";
        private const string SupabaseKey =
            "YOUR_ANON_KEY";

        public bool IsEnabled { get; private set; }
        public DateTime LastSyncTime => _lastSyncTime;
        public string SyncStatus { get; private set; } = "غير متصل";

        public event Action<string>? OnSyncStatusChanged;

        public CloudSyncService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add(
                "apikey", SupabaseKey);
            _httpClient.DefaultRequestHeaders.Add(
                "Authorization", $"Bearer {SupabaseKey}");

            // مزامنة كل 10 دقائق
            _syncTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(10)
            };
            _syncTimer.Tick += async (s, e) => await SyncAsync();
        }

        public void Enable()
        {
            IsEnabled = true;
            _syncTimer.Start();
            _ = Task.Run(async () => await SyncAsync());
        }

        public void Disable()
        {
            IsEnabled = false;
            _syncTimer.Stop();
            UpdateStatus("مُعطَّل");
        }

        // ─── المزامنة الرئيسية ───────────────────────
        public async Task<bool> SyncAsync()
        {
            if (_isSyncing || !IsEnabled) return false;

            _isSyncing = true;
            UpdateStatus("جاري المزامنة...");

            try
            {
                // التحقق من الاتصال
                bool isConnected = await CheckInternetAsync();
                if (!isConnected)
                {
                    UpdateStatus("لا يوجد إنترنت");
                    return false;
                }

                // مزامنة البيانات
                await SyncDashboardDataAsync();

                _lastSyncTime = DateTime.Now;
                UpdateStatus(
                    $"آخر مزامنة: {_lastSyncTime:HH:mm}");

                return true;
            }
            catch (Exception ex)
            {
                UpdateStatus($"خطأ: {ex.Message}");
                return false;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        // ─── مزامنة بيانات الـ Dashboard ─────────────
        private async Task SyncDashboardDataAsync()
        {
            try
            {
                // جمع البيانات من قاعدة البيانات المحلية
                using var context = new Data.AppDbContext(
                    Data.DatabaseConfig.GetOptions());

                var today = DateTime.Today;
                var monthStart = new DateTime(
                    today.Year, today.Month, 1);

                // إحصائيات اليوم
                var todaySales = await context.Orders
                    .Where(o => o.OrderDate.Date == today &&
                                !o.IsDeleted)
                    .SumAsync(o => o.TotalAmount);

                var todayOrders = await context.Orders
                    .CountAsync(o =>
                        o.OrderDate.Date == today &&
                        !o.IsDeleted);

                var todayProduction = await context.ProductionRecords
                    .Where(p => p.ProductionDate.Date == today)
                    .SumAsync(p => p.QuantityNet);

                var totalDebt = await context.Customers
                    .SumAsync(c => c.TotalDebt);

                var cashBalance = (await context.Accounts
                    .FirstOrDefaultAsync(a => a.Code == "1001"))
                    ?.Balance ?? 0;

                // إرسال للسحابة
                var payload = new
                {
                    factory_id = "main",
                    updated_at = DateTime.UtcNow,
                    today_sales = todaySales,
                    today_orders = todayOrders,
                    today_production = todayProduction,
                    total_debt = totalDebt,
                    cash_balance = cashBalance
                };

                await _httpClient.PostAsJsonAsync(
                    $"{SupabaseUrl}/rest/v1/dashboard_sync",
                    payload);
            }
            catch { }
        }

        // ─── التحقق من الإنترنت ──────────────────────
        private async Task<bool> CheckInternetAsync()
        {
            try
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };
                var response = await client.GetAsync(
                    "http://www.google.com");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateStatus(string status)
        {
            SyncStatus = status;
            OnSyncStatusChanged?.Invoke(status);
        }
    }
}
