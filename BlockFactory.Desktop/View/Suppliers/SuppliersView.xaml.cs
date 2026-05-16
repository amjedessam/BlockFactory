using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Desktop.ViewModels.Suppliers;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BlockFactory.Desktop.Views.Suppliers
{
    public partial class SuppliersView : UserControl
    {
        private readonly SuppliersViewModel _viewModel;

        public SuppliersView()
        {
            InitializeComponent();
            _viewModel = App.GetService<SuppliersViewModel>();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender,
            System.Windows.RoutedEventArgs e)
            => await _viewModel.LoadAsync();
    }
}
