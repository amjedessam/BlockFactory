using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Desktop.ViewModels.HR;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BlockFactory.Desktop.Views.HR
{
    public partial class WorkersView : UserControl
    {
        private readonly WorkersViewModel _viewModel;

        public WorkersView()
        {
            InitializeComponent();
            _viewModel = App.GetService<WorkersViewModel>();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender,
            System.Windows.RoutedEventArgs e)
            => await _viewModel.LoadAsync();
    }
}
