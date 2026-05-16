using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Desktop.ViewModels.Auth;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace BlockFactory.Desktop
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow()
        {
            InitializeComponent();

            _viewModel = App.GetService<LoginViewModel>();
            DataContext = _viewModel;

            // التركيز على حقل اسم المستخدم
            Loaded += (s, e) => UsernameBox.Focus();
        }

        // ─── سحب النافذة ───────────────────────────
        private void TitleBar_MouseLeftButtonDown(object sender,
            MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        // ─── إغلاق النافذة ─────────────────────────
        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        // ─── Enter للدخول ───────────────────────────
        private void InputField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                PasswordBox.Focus();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_viewModel.LoginCommand.CanExecute(PasswordBox))
                    _viewModel.LoginCommand.Execute(PasswordBox);
            }
        }
    }
}
