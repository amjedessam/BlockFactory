using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Desktop.ViewModels.Reports;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BlockFactory.Desktop.Views.Reports
{
    public partial class ReportsView : UserControl
    {
        private readonly ReportsViewModel _viewModel;

        public ReportsView()
        {
            InitializeComponent();
            _viewModel = App.GetService<ReportsViewModel>();
            DataContext = _viewModel;
        }
    }
}
     