// BlockFactory.Desktop/Views/Reservations/NewReservationView.xaml.cs

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlockFactory.Core.DTOs.Customers;
using BlockFactory.Desktop.ViewModels.Reservations;

namespace BlockFactory.Desktop.Views.Reservations
{
    public partial class NewReservationView : UserControl
    {
        private readonly NewReservationViewModel _viewModel;

        public NewReservationView()
        {
            InitializeComponent();
            _viewModel = App.GetService<NewReservationViewModel>();
            DataContext = _viewModel;

            // ✅ تسند ConfirmRequested بنفس أسلوب واجهة المبيعات (MVVM-safe)
            // ViewModel لا يعرف أي شيء عن MessageBox — القرار في الـ View فقط
            _viewModel.ConfirmRequested = async (message) =>
            {
                var result = MessageBox.Show(
                    message,
                    "تحذير — المخزون غير كافٍ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);        // الزر الافتراضي = لا

                return await Task.FromResult(result == MessageBoxResult.Yes);
            };
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadProductsCommand.ExecuteAsync(null);
        }

        // تحديد العميل عند الضغط على أي نتيجة في القائمة
        private void CustomerListBox_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox lb &&
                lb.SelectedItem is CustomerLookupDto customer)
            {
                _viewModel.SelectedCustomer = customer;
            }
        }
    }
}