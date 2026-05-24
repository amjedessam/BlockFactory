using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlockFactory.Core.DTOs.Customers;
using BlockFactory.Desktop.ViewModels.Orders;

namespace BlockFactory.Desktop.Views.Orders
{
    public partial class NewOrderView : UserControl
    {
        private readonly NewOrderViewModel _viewModel;

        public NewOrderView()
        {
            InitializeComponent();
            _viewModel = App.GetService<NewOrderViewModel>();
            DataContext = _viewModel;

            _viewModel.PrintRequested = () =>
            {
                var result = MessageBox.Show(
                    "هل تريد طباعة الفاتورة؟",
                    "طباعة",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.Yes,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                return Task.FromResult(result == MessageBoxResult.Yes);
            };
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadDataCommand.ExecuteAsync(null);
        }

        // ✅ الإصلاح الجوهري — نتجاوز binding تماماً
        private void CustomerListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox lb &&
                lb.SelectedItem is CustomerLookupDto customer)
            {
                _viewModel.SelectedCustomer = customer;
            }
        }
    }
}
/*
namespace BlockFactory.Desktop.Views.Orders
{
    public partial class NewOrderView : UserControl
    {
        private readonly NewOrderViewModel _viewModel;

        public NewOrderView()
        {
            InitializeComponent();
            _viewModel = App.GetService<NewOrderViewModel>();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender,
            System.Windows.RoutedEventArgs e)
        {
            await _viewModel.LoadDataCommand.ExecuteAsync(null);
        }
    }
}*/

