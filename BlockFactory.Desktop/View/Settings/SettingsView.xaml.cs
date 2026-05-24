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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // يمكن تحديث حالة الـ API عند كل ظهور للشاشة
        }

        // ── زر نسخ رابط API ──
        private void CopyApiUrl_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_viewModel.ApiUrl))
            {
                Clipboard.SetText(_viewModel.ApiUrl);
                MessageBox.Show(
                    $"تم نسخ الرابط:\n{_viewModel.ApiUrl}",
                    "تم النسخ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information,
                    MessageBoxResult.OK,
                    MessageBoxOptions.RightAlign |
                    MessageBoxOptions.RtlReading);
            }
        }
    }
}

/*
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

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // يمكن لاحقاً: تحديث حالة الـ API أو النسخ عند كل ظهور للشاشة
        }
    }
}
*/