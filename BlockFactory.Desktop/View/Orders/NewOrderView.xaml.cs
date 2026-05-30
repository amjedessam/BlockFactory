using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlockFactory.Core.DTOs.Customers;
using BlockFactory.Desktop.ViewModels.Orders;

namespace BlockFactory.Desktop.Views.Orders
{
    public partial class NewOrderView : UserControl
    {
        private readonly NewOrderViewModel _viewModel;

        public NewOrderView()
        {
            InitializeComponent();
            _viewModel = App.GetService<NewOrderViewModel>();
            DataContext = _viewModel;

            _viewModel.PrintRequested = () =>
            {
                var result = MessageBox.Show(
                    "هل تريد طباعة الفاتورة؟",
                    "طباعة",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.Yes,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                return Task.FromResult(result == MessageBoxResult.Yes);
            };

            _viewModel.ConfirmRequested = (msg) =>
            {
                var result = MessageBox.Show(
                    msg,
                    "تنبيه المخزون",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                return Task.FromResult(result == MessageBoxResult.Yes);
            };
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadDataCommand.ExecuteAsync(null);

            // ─── نربط الكيبورد على مستوى الـ Window كاملاً ───
            // الـ Popup يسرق الـ Focus ويمنع KeyDown من الوصول للـ TextBox مباشرة
            var window = Window.GetWindow(this);
            if (window != null)
                window.PreviewKeyDown += Window_PreviewKeyDown;
        }

        // ─── PreviewKeyDown على الـ Window — يُطلق قبل أي عنصر آخر ──────────
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // تشتغل فقط إذا كانت القائمة مفتوحة (فيها نتائج)
            if (_viewModel.CustomerResults.Count == 0) return;

            // تشتغل فقط إذا كان حقل البحث أو القائمة هو محور التركيز
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

                // تمرير القائمة للعنصر المحدد تلقائياً
                if (!isEnter && _viewModel.HighlightedCustomerIndex >= 0)
                {
                    CustomerListBox.UpdateLayout();
                    var item = CustomerListBox.ItemContainerGenerator
                        .ContainerFromIndex(_viewModel.HighlightedCustomerIndex)
                        as ListBoxItem;
                    item?.BringIntoView();
                }

                // إعادة الـ Focus للـ TextBox دائماً
                CustomerSearchBox.Focus();
                e.Handled = true;
            }
        }

        // ─── helper: هل العنصر فرع من container معين ────────────────────────
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

        // ─── اختيار العميل بالنقر بالماوس ───────────────────────────────────
        private void CustomerListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox lb &&
                lb.SelectedItem is CustomerLookupDto customer)
            {
                _viewModel.SelectedCustomer = customer;
            }
        }
    }
}
