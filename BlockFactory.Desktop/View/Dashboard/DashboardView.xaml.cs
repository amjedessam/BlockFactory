using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Desktop.ViewModels.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BlockFactory.Desktop.Views.Dashboard
{
    public partial class DashboardView : UserControl
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardView()
        {
            InitializeComponent();

            _viewModel = App.GetService<DashboardViewModel>();
            DataContext = _viewModel;
        }

        // تحميل البيانات عند ظهور الصفحة
        private async void UserControl_Loaded(object sender,
            System.Windows.RoutedEventArgs e)
        {
            await _viewModel.LoadDataAsync();
        }
    }
}
