/*using System.Windows;
using System.Windows.Controls;
using BlockFactory.Desktop.ViewModels.Settings;

namespace BlockFactory.Desktop.Views.Settings
{
    public partial class SettingsView : UserControl
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsView()
        {
            InitializeComponent();
            _viewModel = App.GetService<SettingsViewModel>();
            DataContext = _viewModel;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // يمكن تحديث حالة الـ API عند كل ظهور للشاشة
        }

    }
}*/


// BlockFactory.Desktop/Views/Settings/SettingsView.xaml.cs

using System.Windows;
using System.Windows.Controls;
using BlockFactory.Desktop.ViewModels.Settings;

namespace BlockFactory.Desktop.Views.Settings
{
    public partial class SettingsView : UserControl
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsView()
        {
            InitializeComponent();
            _viewModel = App.GetService<SettingsViewModel>();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadProductsAsync();
            await _viewModel.LoadInventoryAsync();
        }

        // ─── Tab Switching ───────────────────────────

        private void TabPrices_Click(object sender, RoutedEventArgs e)
        {
            TabPricesContent.Visibility = Visibility.Visible;
            TabInventoryContent.Visibility = Visibility.Collapsed;
            TabRawMaterialsContent.Visibility = Visibility.Collapsed;

            BtnTabPrices.Style = (Style)FindResource("TabBtnActive");
            BtnTabInventory.Style = (Style)FindResource("TabBtn");
            BtnTabRawMaterials.Style = (Style)FindResource("TabBtn");
        }

        private void TabInventory_Click(object sender, RoutedEventArgs e)
        {
            TabPricesContent.Visibility = Visibility.Collapsed;
            TabInventoryContent.Visibility = Visibility.Visible;
            TabRawMaterialsContent.Visibility = Visibility.Collapsed;

            BtnTabPrices.Style = (Style)FindResource("TabBtn");
            BtnTabInventory.Style = (Style)FindResource("TabBtnActive");
            BtnTabRawMaterials.Style = (Style)FindResource("TabBtn");
        }

        private void TabRawMaterials_Click(object sender, RoutedEventArgs e)
        {
            TabPricesContent.Visibility = Visibility.Collapsed;
            TabInventoryContent.Visibility = Visibility.Collapsed;
            TabRawMaterialsContent.Visibility = Visibility.Visible;

            BtnTabPrices.Style = (Style)FindResource("TabBtn");
            BtnTabInventory.Style = (Style)FindResource("TabBtn");
            BtnTabRawMaterials.Style = (Style)FindResource("TabBtnActive");
        }
    }
}