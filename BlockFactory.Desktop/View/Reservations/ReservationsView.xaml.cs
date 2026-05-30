// BlockFactory.Desktop/Views/Reservations/ReservationsView.xaml.cs

using System.Windows;
using System.Windows.Controls;
using BlockFactory.Desktop.ViewModels.Reservations;

namespace BlockFactory.Desktop.Views.Reservations
{
    public partial class ReservationsView : UserControl
    {
        private readonly ReservationsViewModel _viewModel;

        public ReservationsView()
        {
            InitializeComponent();
            _viewModel = App.GetService<ReservationsViewModel>();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadAsync();
        }
    }
}
