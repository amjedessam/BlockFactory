// BlockFactory.Desktop/Views/Reservations/NewReservationView.xaml.cs

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlockFactory.Core.DTOs.Customers;
using BlockFactory.Desktop.ViewModels.Reservations;

namespace BlockFactory.Desktop.Views.Reservations
{
    public partial class NewReservationView : UserControl
    {
        private readonly NewReservationViewModel _viewModel;

        public NewReservationView()
        {
            InitializeComponent();
            _viewModel = App.GetService<NewReservationViewModel>();
            DataContext = _viewModel;

            // ─── تحذير المخزون ───────────────────────────────────────────
            _viewModel.ConfirmRequested = async (message) =>
            {
                var result = MessageBox.Show(
                    message,
                    "تحذير — المخزون غير كافٍ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                return await Task.FromResult(result == MessageBoxResult.Yes);
            };

            // ─── طباعة فاتورة الحجز ──────────────────────────────────────
            _viewModel.PrintRequested = async () =>
            {
                var result = MessageBox.Show(
                    "تم حفظ فاتورة الحجز بنجاح.\nهل تريد طباعة الفاتورة؟",
                    "طباعة فاتورة الحجز",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.Yes);

                return await Task.FromResult(result == MessageBoxResult.Yes);
            };
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadProductsCommand.ExecuteAsync(null);

            // ─── نربط الكيبورد على مستوى الـ Window ──────────────────────
            // الـ Popup يسرق الـ Focus — PreviewKeyDown يعمل قبل أي عنصر
            var window = Window.GetWindow(this);
            if (window != null)
                window.PreviewKeyDown += Window_PreviewKeyDown;
        }

        // ─── PreviewKeyDown على الـ Window ───────────────────────────────
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_viewModel.CustomerResults.Count == 0) return;

            var focused = Keyboard.FocusedElement;
            bool searchHasFocus =
                focused == CustomerSearchBox ||
                IsDescendantOf(focused as DependencyObject, CustomerListBox);

            if (!searchHasFocus) return;

            bool isEnter = e.Key == Key.Enter || e.Key == Key.Return;
            bool isUp = e.Key == Key.Up;
            bool isDown = e.Key == Key.Down;

            if (isDown || isUp || isEnter)
            {
                string keyName = isEnter ? "Enter" : e.Key.ToString();
                _viewModel.HandleCustomerSearchKey(keyName);

                if (!isEnter && _viewModel.HighlightedCustomerIndex >= 0)
                {
                    CustomerListBox.UpdateLayout();
                    var item = CustomerListBox.ItemContainerGenerator
                        .ContainerFromIndex(_viewModel.HighlightedCustomerIndex)
                        as ListBoxItem;
                    item?.BringIntoView();
                }

                CustomerSearchBox.Focus();
                e.Handled = true;
            }
        }

        // ─── helper: هل العنصر فرع من container معين ────────────────────
        private static bool IsDescendantOf(DependencyObject? child,
                                           DependencyObject? parent)
        {
            if (child == null || parent == null) return false;
            var current = child;
            while (current != null)
            {
                if (current == parent) return true;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        // ─── اختيار العميل بالنقر بالماوس ───────────────────────────────
        private void CustomerListBox_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox lb &&
                lb.SelectedItem is CustomerLookupDto customer)
            {
                _viewModel.SelectedCustomer = customer;
            }
        }
    }
}
