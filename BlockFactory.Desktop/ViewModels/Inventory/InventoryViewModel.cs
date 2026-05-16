using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BlockFactory.Core.DTOs.Inventory;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Session;
using BlockFactory.Desktop.ViewModels.Base;

namespace BlockFactory.Desktop.ViewModels.Inventory
{
    public class InventoryViewModel : BaseViewModel
    {
        private readonly IInventoryService _inventory;

        private int _productSkuCount;
        public int ProductSkuCount
        {
            get => _productSkuCount;
            set => SetProperty(ref _productSkuCount, value);
        }

        private int _totalUnits;
        public int TotalUnits
        {
            get => _totalUnits;
            set => SetProperty(ref _totalUnits, value);
        }

        public ObservableCollection<InventoryProductRowDto> LowStockProducts
        { get; } = new();

        public ObservableCollection<InventoryMaterialRowDto> LowRawMaterials
        { get; } = new();

        public InventoryViewModel(IInventoryService inventory)
        {
            _inventory = inventory;
        }

        public bool CanAdjustStock =>
            CurrentSession.Instance.HasPermission("AdjustStock");

        public async Task LoadAsync()
        {
            var summary = await _inventory.GetSummaryAsync();
            ProductSkuCount = summary.ProductSkuCount;
            TotalUnits = summary.TotalUnitsAvailable;

            LowStockProducts.Clear();
            foreach (var row in await _inventory.GetLowStockProductsAsync())
                LowStockProducts.Add(row);

            LowRawMaterials.Clear();
            foreach (var row in await _inventory.GetLowRawMaterialsAsync())
                LowRawMaterials.Add(row);
        }
    }
}
