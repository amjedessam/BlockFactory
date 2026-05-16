using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Desktop.ViewModels.Finance;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BlockFactory.Desktop.Views.Finance
{
    public partial class FinanceView : UserControl
    {
        private readonly FinanceViewModel _viewModel;

        public FinanceView()
        {
            InitializeComponent();
            _viewModel = App.GetService<FinanceViewModel>();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender,
            System.Windows.RoutedEventArgs e)
            => await _viewModel.LoadAsync();
    }
}
