using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.DTOs.Dashboard;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Session;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace BlockFactory.Desktop.ViewModels.Dashboard
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IDashboardService _dashboardService;
        private readonly DispatcherTimer _refreshTimer;

        public DashboardViewModel(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;

            // تحديث تلقائي كل 5 دقائق
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _refreshTimer.Tick += async (s, e)
                => await LoadDataAsync();
            _refreshTimer.Start();
        }

        // ─── Properties — إحصائيات ──────────────────

        private decimal _todaySales;
        public decimal TodaySales
        {
            get => _todaySales;
            set => SetProperty(ref _todaySales, value);
        }

        private decimal _monthSales;
        public decimal MonthSales
        {
            get => _monthSales;
            set => SetProperty(ref _monthSales, value);
        }

        private int _todayOrdersCount;
        public int TodayOrdersCount
        {
            get => _todayOrdersCount;
            set => SetProperty(ref _todayOrdersCount, value);
        }

        private decimal _totalCustomerDebt;
        public decimal TotalCustomerDebt
        {
            get => _totalCustomerDebt;
            set => SetProperty(ref _totalCustomerDebt, value);
        }

        private int _pledgesDueSoonCount;
        public int PledgesDueSoonCount
        {
            get => _pledgesDueSoonCount;
            set => SetProperty(ref _pledgesDueSoonCount, value);
        }

        private int _todayProduction;
        public int TodayProduction
        {
            get => _todayProduction;
            set => SetProperty(ref _todayProduction, value);
        }

        private int _lowStockCount;
        public int LowStockCount
        {
            get => _lowStockCount;
            set => SetProperty(ref _lowStockCount, value);
        }

        private decimal _cashBalance;
        public decimal CashBalance
        {
            get => _cashBalance;
            set => SetProperty(ref _cashBalance, value);
        }

        private decimal _totalWalletsBalance;
        public decimal TotalWalletsBalance
        {
            get => _totalWalletsBalance;
            set => SetProperty(ref _totalWalletsBalance, value);
        }

        private decimal _netProfit;
        public decimal NetProfit
        {
            get => _netProfit;
            set => SetProperty(ref _netProfit, value);
        }

        private bool _isNetProfitPositive;
        public bool IsNetProfitPositive
        {
            get => _isNetProfitPositive;
            set => SetProperty(ref _isNetProfitPositive, value);
        }

        // ─── بيانات المستخدم ────────────────────────
        public string WelcomeMessage =>
            $"مرحباً، {CurrentSession.Instance.FullName} 👋";

        public string TodayDate => DateTime.Now.ToString(
            "dddd، d MMMM yyyy",
            new System.Globalization.CultureInfo("ar-YE"));

        // ─── Collections ────────────────────────────
        public ObservableCollection<RecentOrderDto> RecentOrders { get; }
            = new();

        public ObservableCollection<AlertDto> Alerts { get; }
            = new();

        private bool _hasAlerts;
        public bool HasAlerts
        {
            get => _hasAlerts;
            set => SetProperty(ref _hasAlerts, value);
        }

        private string _lastUpdated = string.Empty;
        public string LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        // ─── Load Data ──────────────────────────────
        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // تحميل الإحصائيات
                var stats = await _dashboardService.GetStatsAsync();
                ApplyStats(stats);

                // آخر الطلبات
                var orders = await _dashboardService
                    .GetRecentOrdersAsync(8);
                RecentOrders.Clear();
                foreach (var o in orders)
                    RecentOrders.Add(o);

                // التنبيهات
                var alerts = await _dashboardService.GetAlertsAsync();
                Alerts.Clear();
                foreach (var a in alerts)
                    Alerts.Add(a);

                HasAlerts = Alerts.Count > 0;
                LastUpdated = $"آخر تحديث: {DateTime.Now:hh:mm tt}";
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل البيانات: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyStats(DashboardStatsDto stats)
        {
            TodaySales = stats.TodaySales;
            MonthSales = stats.MonthSales;
            TodayOrdersCount = stats.TodayOrdersCount;
            TotalCustomerDebt = stats.TotalCustomerDebt;
            PledgesDueSoonCount = stats.PledgesDueSoonCount;
            TodayProduction = stats.TodayProduction;
            LowStockCount = stats.LowStockProductsCount
                          + stats.LowRawMaterialsCount;
            CashBalance = stats.CashBalance;
            TotalWalletsBalance = stats.TotalWalletsBalance;
            NetProfit = stats.NetProfit;
            IsNetProfitPositive = stats.NetProfit >= 0;
        }
    }
}
