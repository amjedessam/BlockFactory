using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Desktop.ViewModels.Production;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BlockFactory.Desktop.Views.Production
{
    public partial class ProductionView : UserControl
    {
        private readonly ProductionViewModel _viewModel;

        public ProductionView()
        {
            InitializeComponent();
            _viewModel = App.GetService<ProductionViewModel>();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender,
            System.Windows.RoutedEventArgs e)
            => await _viewModel.LoadAllAsync();
    }
}
