using System.Windows.Controls;
using BlockFactory.Desktop.ViewModels.Inventory;

namespace BlockFactory.Desktop.Views.Inventory
{
    public partial class InventoryView : UserControl
    {
        private readonly InventoryViewModel _viewModel;

        public InventoryView()
        {
            InitializeComponent();
            _viewModel = App.GetService<InventoryViewModel>();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender,
            System.Windows.RoutedEventArgs e)
            => await _viewModel.LoadAsync();
    }
}
