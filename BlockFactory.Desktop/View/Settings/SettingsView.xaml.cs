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
