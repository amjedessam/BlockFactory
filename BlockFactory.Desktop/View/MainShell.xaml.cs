using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BlockFactory.Desktop.Services;
using BlockFactory.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BlockFactory.Desktop
{
    public partial class MainShell : Window
    {
        private readonly MainShellViewModel _viewModel;
        private readonly NavigationService _navigationService;

        public MainShell()
        {
            InitializeComponent();

            _viewModel = App.GetService<MainShellViewModel>();
            _navigationService = App.GetService<NavigationService>();

            // ربط الـ Frame بالـ NavigationService
            _navigationService.SetFrame(MainFrame);

            DataContext = _viewModel;

            // تشغيل الساعة
            _viewModel.StartClock();
        }
    }
}
