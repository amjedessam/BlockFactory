using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using BlockFactory.Desktop.ViewModels.Settings;

namespace BlockFactory.Desktop.Views.Settings
{
    public partial class UsersView : UserControl
    {
        private readonly UsersViewModel _viewModel;

        public UsersView()
        {
            InitializeComponent();
            _viewModel = App.GetService<UsersViewModel>();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
            => await _viewModel.LoadAsync();

        private void AccountantPassword_OnPasswordChanged(
            object sender, RoutedEventArgs e)
            => CommandManager.InvalidateRequerySuggested();

        private void AccountantForm_IsVisibleChanged(
            object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement ui &&
                ui.Visibility == Visibility.Visible)
                AccountantPasswordBox.Password = string.Empty;
        }

        private void CancelAccountantForm_Click(
            object sender, RoutedEventArgs e)
        {
            AccountantPasswordBox.Password = string.Empty;
            _viewModel.CancelAddAccountantFormCommand.Execute(null);
        }
    }
}
