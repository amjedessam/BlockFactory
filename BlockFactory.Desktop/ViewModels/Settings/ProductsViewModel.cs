using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Desktop.ViewModels.Base;

namespace BlockFactory.Desktop.ViewModels.Settings
{
    public class ProductListRow
    {
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public int Size { get; set; }
        public decimal DefaultPrice { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProductsViewModel : BaseViewModel
    {
        private readonly IProductService _products;

        public ObservableCollection<ProductListRow> Products { get; } = new();

        public ProductsViewModel(IProductService products)
        {
            _products = products;
        }

        public async Task LoadAsync()
        {
            var list = await _products.GetActiveProductsAsync();
            Products.Clear();
            foreach (var p in list.OrderBy(x => x.Name))
            {
                Products.Add(new ProductListRow
                {
                    Name = p.Name,
                    TypeName = p.ProductType?.Name ?? "-",
                    Size = p.Size,
                    DefaultPrice = p.DefaultPrice,
                    IsActive = p.IsActive
                });
            }
        }
    }
}
