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
}
