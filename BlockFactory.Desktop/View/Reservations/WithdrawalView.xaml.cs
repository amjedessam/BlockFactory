// BlockFactory.Desktop/Views/Reservations/WithdrawalView.xaml.cs

using System.Windows.Controls;
using BlockFactory.Desktop.ViewModels.Reservations;

namespace BlockFactory.Desktop.Views.Reservations
{
    public partial class WithdrawalView : UserControl
    {
        private readonly WithdrawalViewModel _viewModel;

        public WithdrawalView()
        {
            InitializeComponent();
            _viewModel = App.GetService<WithdrawalViewModel>();
            DataContext = _viewModel;
        }

        /// <summary>
        /// يُستدعى من ReservationsViewModel عند الضغط على زر "تنفيذ سحب"
        /// يمرر ReservationId للـ ViewModel
        /// </summary>
        public void LoadReservation(int reservationId)
        {
            _viewModel.ReservationId = reservationId;
        }
    }
}
