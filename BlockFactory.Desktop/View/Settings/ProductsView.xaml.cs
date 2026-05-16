using System.Windows.Controls;

using BlockFactory.Desktop.ViewModels.Settings;

namespace BlockFactory.Desktop.Views.Settings
{
    public partial class ProductsView : UserControl
    {
        private readonly ProductsViewModel _viewModel;

        public ProductsView()
        {
            InitializeComponent();
            _viewModel = App.GetService<ProductsViewModel>();
            DataContext = _viewModel;
        }

        private async void UserControl_Loaded(object sender,
            System.Windows.RoutedEventArgs e)
            => await _viewModel.LoadAsync();
    }
}
