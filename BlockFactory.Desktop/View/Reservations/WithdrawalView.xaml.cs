// BlockFactory.Desktop/Views/Reservations/WithdrawalView.xaml.cs

using System.Windows;
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

            // ─── طباعة فاتورة السحب (نفس نمط NewOrderView) ──────────────
            _viewModel.PrintRequested = async () =>
            {
                var result = MessageBox.Show(
                    "تم حفظ عملية السحب بنجاح.\nهل تريد طباعة فاتورة السحب؟",
                    "طباعة فاتورة السحب",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.Yes);

                return await Task.FromResult(result == MessageBoxResult.Yes);
            };
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