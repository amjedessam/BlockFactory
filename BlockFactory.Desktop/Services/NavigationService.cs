
// BlockFactory.Desktop/Services/NavigationService.cs

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Desktop.Views.Customers;
using BlockFactory.Desktop.Views.Dashboard;
using BlockFactory.Desktop.Views.Orders;

using Microsoft.Extensions.DependencyInjection;

namespace BlockFactory.Desktop.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Stack<string> _history = new();
        private ContentControl? _frame;

        public string CurrentView { get; private set; } = string.Empty;
        public event Action<string>? OnNavigated;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void SetFrame(ContentControl frame)
            => _frame = frame;

        public void NavigateTo(string viewName)
        {
            if (_frame == null) return;

            if (!ViewAccess.CanNavigate(viewName))
            {
                MessageBox.Show(
                    "ليس لديك صلاحية للوصول إلى هذه الشاشة.",
                    "صلاحيات",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning,
                    MessageBoxResult.OK,
                    MessageBoxOptions.RightAlign |
                    MessageBoxOptions.RtlReading);
                return;
            }

            var view = ResolveView(viewName);
            if (view == null) return;

            if (!string.IsNullOrEmpty(CurrentView))
                _history.Push(CurrentView);

            CurrentView = viewName;
            _frame.Content = view;
            OnNavigated?.Invoke(viewName);
        }

        public void NavigateBack()
        {
            if (_history.Count == 0) return;
            var previous = _history.Pop();
            NavigateTo(previous);
        }

        private UserControl ResolveView(string viewName)
        {
            // ─── Views جاهزة ────────────────────────────
            UserControl? view = viewName switch
            {
                "Dashboard" => _serviceProvider.GetService<DashboardView>(),
                "Orders" => _serviceProvider.GetService<OrdersView>(),
                "NewOrder" => _serviceProvider.GetService<NewOrderView>(),
                "Customers" => _serviceProvider.GetService<CustomersView>(),
                "Pledges" => _serviceProvider.GetService<PledgesView>(),
                _ => null
            };

            // ─── Views غير جاهزة بعد → شاشة "قريباً" ───
            return view ?? new ComingSoonView(viewName);
        }
    }

    // ─── شاشة مؤقتة للـ Views غير المكتملة ─────────────
    public class ComingSoonView : UserControl
    {
        private static readonly Dictionary<string, string> _names = new()
        {
            ["Production"] = "الإنتاج",
            ["Inventory"] = "المخزون",
            ["Suppliers"] = "الموردون",
            ["Workers"] = "العمال",
            ["Salaries"] = "الرواتب",
            ["Finance"] = "المالية",
            ["Reports"] = "التقارير",
            ["Products"] = "المنتجات",
            ["Users"] = "المستخدمون",
            ["Settings"] = "الإعدادات",
        };

        public ComingSoonView(string viewName)
        {
            _names.TryGetValue(viewName, out var arabicName);
            arabicName ??= viewName;

            FlowDirection = FlowDirection.RightToLeft;
            Background = new SolidColorBrush(
                Color.FromRgb(245, 247, 250));

            Content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text               = "🚧",
                        FontSize           = 64,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin             = new Thickness(0, 0, 0, 16)
                    },
                    new TextBlock
                    {
                        Text               = $"شاشة {arabicName}",
                        FontSize           = 28,
                        FontWeight         = FontWeights.Bold,
                        Foreground         = new SolidColorBrush(
                            Color.FromRgb(30, 58, 95)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin             = new Thickness(0, 0, 0, 8)
                    },
                    new TextBlock
                    {
                        Text               = "هذه الشاشة قيد التطوير وستكون جاهزة قريباً",
                        FontSize           = 16,
                        Foreground         = new SolidColorBrush(
                            Color.FromRgb(120, 130, 145)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                }
            };
        }
    }
}
/*using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Session;
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

using Microsoft.Extensions.DependencyInjection;

namespace BlockFactory.Desktop.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Stack<string> _history = new();
        private ContentControl? _frame;

        public string CurrentView { get; private set; } = string.Empty;
        public event Action<string>? OnNavigated;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void SetFrame(ContentControl frame)
            => _frame = frame;

        public void NavigateTo(string viewName)
        {
            if (_frame == null) return;

            if (!ViewAccess.CanNavigate(viewName))
            {
                MessageBox.Show(
                    "ليس لديك صلاحية للوصول إلى هذه الشاشة.",
                    "صلاحيات",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning,
                    MessageBoxResult.OK,
                    MessageBoxOptions.RightAlign |
                    MessageBoxOptions.RtlReading);
                return;
            }

            var view = ResolveView(viewName);
            if (view == null) return;

            if (!string.IsNullOrEmpty(CurrentView))
                _history.Push(CurrentView);

            CurrentView = viewName;
            _frame.Content = view;
            OnNavigated?.Invoke(viewName);
        }

        public void NavigateBack()
        {
            if (_history.Count == 0) return;
            var previous = _history.Pop();
            NavigateTo(previous);
        }

        private UserControl? ResolveView(string viewName) => viewName switch
        {
            "Dashboard" => _serviceProvider
                .GetRequiredService<DashboardView>(),
            "Orders" => _serviceProvider.GetRequiredService<OrdersView>(),
            "NewOrder" => _serviceProvider.GetRequiredService<NewOrderView>(),
            "Customers" => _serviceProvider
                .GetRequiredService<CustomersView>(),
            "Pledges" => _serviceProvider.GetRequiredService<PledgesView>(),
            "Production" => _serviceProvider
                .GetRequiredService<ProductionView>(),
            "Inventory" => _serviceProvider
                .GetRequiredService<InventoryView>(),
            "Suppliers" => _serviceProvider
                .GetRequiredService<SuppliersView>(),
            "Workers" => _serviceProvider.GetRequiredService<WorkersView>(),
            "Salaries" => _serviceProvider.GetRequiredService<SalariesView>(),
            "Finance" => _serviceProvider.GetRequiredService<FinanceView>(),
            "Reports" => _serviceProvider.GetRequiredService<ReportsView>(),
            "Products" => _serviceProvider.GetRequiredService<ProductsView>(),
            "Users" => _serviceProvider.GetRequiredService<UsersView>(),
            "Settings" => _serviceProvider
                .GetRequiredService<SettingsView>(),
            _ => null
        };
    }
}*/
