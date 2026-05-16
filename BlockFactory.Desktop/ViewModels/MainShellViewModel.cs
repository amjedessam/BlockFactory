using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Session;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.Models;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;

namespace BlockFactory.Desktop.ViewModels
{
    public class MainShellViewModel : BaseViewModel
    {
        private readonly INavigationService _navigation;
        private readonly IAuthService _authService;

        public MainShellViewModel(
            INavigationService navigation,
            IAuthService authService)
        {
            _navigation = navigation;
            _authService = authService;

            _navigation.OnNavigated += OnNavigated;

            InitializeMenuGroups();
            InitializeCommands();
            LoadFavorites();

            // الانتقال للداشبورد عند الفتح
            NavigateCommand.Execute("Dashboard");
        }

        // ─── Properties ────────────────────────────

        private string _currentViewTitle = "لوحة التحكم";
        public string CurrentViewTitle
        {
            get => _currentViewTitle;
            set => SetProperty(ref _currentViewTitle, value);
        }

        private string _currentTime = string.Empty;
        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        private bool _isSidebarExpanded = true;
        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            set
            {
                SetProperty(ref _isSidebarExpanded, value);
                OnPropertyChanged(nameof(SidebarWidth));
            }
        }

        public double SidebarWidth => IsSidebarExpanded ? 250 : 64;

        // بيانات المستخدم الحالي
        public string UserFullName
            => CurrentSession.Instance.FullName;

        public string UserRole 
            => CurrentSession.Instance.IsAdmin
               ? "مدير النظام" : "محاسب";

        public string UserInitials
            => CurrentSession.Instance.FullName.Length >= 2
               ? CurrentSession.Instance.FullName[..2]
               : CurrentSession.Instance.FullName;

        public bool IsAdmin => CurrentSession.Instance.IsAdmin;

        // قائمة التنقل
        public ObservableCollection<NavigationMenuGroup> MenuGroups { get; }
            = new();

        // المفضلة
        public ObservableCollection<NavigationMenuItem> FavoriteItems { get; }
            = new();

        private bool _hasFavorites;
        public bool HasFavorites
        {
            get => _hasFavorites;
            set => SetProperty(ref _hasFavorites, value);
        }

        // ─── Commands ───────────────────────────────
        public RelayCommand NavigateCommand { get; private set; } = null!;
        public RelayCommand ToggleSidebarCommand { get; private set; } = null!;
        public RelayCommand ToggleFavoriteCommand { get; private set; } = null!;
        public RelayCommand ToggleGroupCommand { get; private set; } = null!;
        public AsyncRelayCommand LogoutCommand { get; private set; } = null!;

        // ─── Init ───────────────────────────────────

        private void InitializeCommands()
        {
            NavigateCommand = new RelayCommand(param =>
            {
                if (param is string viewName)
                    _navigation.NavigateTo(viewName);
            });

            ToggleSidebarCommand = new RelayCommand(_ =>
                IsSidebarExpanded = !IsSidebarExpanded);

            ToggleFavoriteCommand = new RelayCommand(param =>
            {
                if (param is NavigationMenuItem item)
                {
                    item.IsFavorite = !item.IsFavorite;
                    RebuildFavorites();
                    SaveFavorites();
                }
            });

            ToggleGroupCommand = new RelayCommand(param =>
            {
                if (param is NavigationMenuGroup group)
                    group.IsExpanded = !group.IsExpanded;
            });

            LogoutCommand = new AsyncRelayCommand(async _ =>
            {
                var result = MessageBox.Show(
                    "هل تريد تسجيل الخروج؟",
                    "تسجيل الخروج",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No,
                    MessageBoxOptions.RightAlign |
                    MessageBoxOptions.RtlReading
                );

                if (result == MessageBoxResult.Yes)
                {
                    await _authService.LogoutAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var loginWindow = new LoginWindow();
                        loginWindow.Show();

                        foreach (Window w in Application.Current.Windows)
                        {
                            if (w is MainShell)
                            {
                                w.Close();
                                break;
                            }
                        }
                    });
                }
            });
        }

        private void InitializeMenuGroups()
        {
            // ─── المجموعة الأولى — الرئيسية ─────────
            var mainGroup = new NavigationMenuGroup
            {
                GroupTitle = "الرئيسية",
                GroupIcon = "🏠",
                Items = new List<NavigationMenuItem>
                {
                    new NavigationMenuItem
                    {
                        Title = "لوحة التحكم",
                        Icon = "📊",
                        ViewName = "Dashboard"
                    }
                }
            };

            // ─── المجموعة الثانية — المبيعات ─────────
            var salesGroup = new NavigationMenuGroup
            {
                GroupTitle = "المبيعات",
                GroupIcon = "💼",
                Items = new List<NavigationMenuItem>
                {
                    new NavigationMenuItem
                    {
                        Title = "الطلبات والفواتير",
                        Icon = "🧾",
                        ViewName = "Orders"
                    },
                    new NavigationMenuItem
                    {
                        Title = "العملاء",
                        Icon = "👥",
                        ViewName = "Customers"
                    }
                }
            };

            // ─── المجموعة الثالثة — التشغيل ──────────
            var operationsGroup = new NavigationMenuGroup
            {
                GroupTitle = "التشغيل",
                GroupIcon = "⚙",
                Items = new List<NavigationMenuItem>
                {
                    new NavigationMenuItem
                    {
                        Title = "الإنتاج اليومي",
                        Icon = "🏭",
                        ViewName = "Production"
                    },
                    new NavigationMenuItem
                    {
                        Title = "المخزون",
                        Icon = "📦",
                        ViewName = "Inventory"
                    },
                    new NavigationMenuItem
                    {
                        Title = "الموردون",
                        Icon = "🚛",
                        ViewName = "Suppliers"
                    }
                }
            };

            // ─── المجموعة الرابعة — الموارد البشرية ──
            var hrGroup = new NavigationMenuGroup
            {
                GroupTitle = "الموارد البشرية",
                GroupIcon = "👷",
                Items = new List<NavigationMenuItem>
                {
                    new NavigationMenuItem
                    {
                        Title = "العمال",
                        Icon = "👷",
                        ViewName = "Workers"
                    },
                    new NavigationMenuItem
                    {
                        Title = "الرواتب والسلف",
                        Icon = "💰",
                        ViewName = "Salaries"
                    }
                }
            };

            // ─── المجموعة الخامسة — المالية ──────────
            var financeGroup = new NavigationMenuGroup
            {
                GroupTitle = "المالية",
                GroupIcon = "📒",
                Items = new List<NavigationMenuItem>
                {
                    new NavigationMenuItem
                    {
                        Title = "المحاسبة",
                        Icon = "📒",
                        ViewName = "Finance"
                    },
                    new NavigationMenuItem
                    {
                        Title = "التقارير",
                        Icon = "📈",
                        ViewName = "Reports"
                    }
                }
            };

            // ─── المجموعة السادسة — الإعدادات (Admin)
            var settingsGroup = new NavigationMenuGroup
            {
                GroupTitle = "الإعدادات",
                GroupIcon = "⚙",
                Items = new List<NavigationMenuItem>()
            };

            if (IsAdmin)
            {
                settingsGroup.Items.Add(new NavigationMenuItem
                {
                    Title = "المنتجات والأسعار",
                    Icon = "🧱",
                    ViewName = "Products"
                });
                settingsGroup.Items.Add(new NavigationMenuItem
                {
                    Title = "إدارة المستخدمين",
                    Icon = "👤",
                    ViewName = "Users"
                });
            }

            MenuGroups.Add(mainGroup);
            MenuGroups.Add(salesGroup);
            MenuGroups.Add(operationsGroup);
            MenuGroups.Add(hrGroup);
            MenuGroups.Add(financeGroup);

            if (settingsGroup.Items.Count > 0)
                MenuGroups.Add(settingsGroup);
        }

        // ─── Favorites ──────────────────────────────

        private void RebuildFavorites()
        {
            FavoriteItems.Clear();
            foreach (var group in MenuGroups)
                foreach (var item in group.Items)
                    if (item.IsFavorite)
                        FavoriteItems.Add(item);

            HasFavorites = FavoriteItems.Count > 0;
        }

        private void SaveFavorites()
        {
            try
            {
                var favViewNames = MenuGroups
                    .SelectMany(g => g.Items)
                    .Where(i => i.IsFavorite)
                    .Select(i => i.ViewName);
                var csv = string.Join(",", favViewNames);

                Properties.Settings.Default.FavoriteMenuItems = csv;
                Properties.Settings.Default.Save();
            }
            catch
            {
                // إذا لم يوجد ملف Settings — لا بأس
            }
        }

        private void LoadFavorites()
        {
            try
            {
                var csv = Properties.Settings.Default.FavoriteMenuItems;
                if (string.IsNullOrEmpty(csv)) return;

                var favSet = new HashSet<string>(
                    csv.Split(',', StringSplitOptions.RemoveEmptyEntries));

                foreach (var group in MenuGroups)
                    foreach (var item in group.Items)
                        if (favSet.Contains(item.ViewName))
                            item.IsFavorite = true;

                RebuildFavorites();
            }
            catch
            {
                // إذا لم يوجد ملف Settings — لا بأس
            }
        }

        // ─── تحديث عنوان الصفحة ─────────────────────
        private void OnNavigated(string viewName)
        {
            CurrentViewTitle = viewName switch
            {
                "Dashboard" => "لوحة التحكم",
                "Orders" => "الطلبات والفواتير",
                "NewOrder" => "طلب جديد",
                "Customers" => "العملاء",
                "Pledges" => "الرهون",
                "Production" => "الإنتاج اليومي",
                "Inventory" => "المخزون",
                "Suppliers" => "الموردون",
                "Workers" => "العمال",
                "Salaries" => "الرواتب والسلف",
                "Finance" => "المحاسبة",
                "Reports" => "التقارير",
                "Products" => "المنتجات والأسعار",
                "Users" => "إدارة المستخدمين",
                "Settings" => "الإعدادات",
                _ => viewName
            };

            // تحديث حالة الاختيار في القائمة
            foreach (var group in MenuGroups)
            {
                bool groupContainsActive = false;
                foreach (var item in group.Items)
                {
                    item.IsSelected = item.ViewName == viewName;
                    if (item.IsSelected)
                        groupContainsActive = true;
                }
                // فتح المجموعة الحاوية على العنصر النشط تلقائياً
                if (groupContainsActive && !group.IsExpanded)
                    group.IsExpanded = true;
            }
        }

        // تحديث الوقت كل دقيقة
        public void StartClock()
        {
            UpdateTime();
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            timer.Tick += (s, e) => UpdateTime();
            timer.Start();
        }

        private void UpdateTime()
        {
            CurrentTime = DateTime.Now.ToString("hh:mm tt",
                new System.Globalization.CultureInfo("ar-YE"));
        }
    }
}
