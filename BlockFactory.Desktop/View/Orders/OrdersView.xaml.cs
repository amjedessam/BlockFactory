using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Desktop.ViewModels.Orders;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BlockFactory.Desktop.Views.Orders
{
    public partial class OrdersView : UserControl
    {
        private readonly OrdersViewModel _viewModel;

        public OrdersView()
        {
            InitializeComponent();
            _viewModel = App.GetService<OrdersViewModel>();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender,
            System.Windows.RoutedEventArgs e)
        {
            await _viewModel.LoadOrdersAsync();
        }
    }
}
