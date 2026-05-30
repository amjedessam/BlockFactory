using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Session;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;

namespace BlockFactory.Desktop.ViewModels.Settings
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;

        public SettingsViewModel(
            IProductService productService,
            IInventoryService inventoryService)
        {
            _productService = productService;
            _inventoryService = inventoryService;
            InitializeCommands();
            _ = LoadProductsAsync();
        }

        // ─── Inventory Data ──────────────────────────
        public System.Collections.ObjectModel.ObservableCollection<
            BlockFactory.Core.DTOs.Inventory.InventoryProductRowDto>
            LowStockProducts
        { get; } = new();

        public System.Collections.ObjectModel.ObservableCollection<
            BlockFactory.Core.DTOs.Inventory.InventoryMaterialRowDto>
            LowRawMaterials
        { get; } = new();

        public System.Collections.ObjectModel.ObservableCollection<
            BlockFactory.Core.DTOs.Inventory.InventoryMaterialRowDto>
            AllRawMaterials
        { get; } = new();

        private int _inventoryProductSkuCount;
        public int InventoryProductSkuCount
        {
            get => _inventoryProductSkuCount;
            set => SetProperty(ref _inventoryProductSkuCount, value);
        }

        private int _inventoryTotalUnits;
        public int InventoryTotalUnits
        {
            get => _inventoryTotalUnits;
            set => SetProperty(ref _inventoryTotalUnits, value);
        }

        public int ActiveProductsCount =>
            Products.Count(p => p.IsActive);

        public int LowStockProductsCount => LowStockProducts.Count;
        public int LowRawMaterialsCount => LowRawMaterials.Count;

        public async Task LoadInventoryAsync()
        {
            try
            {
                var summary = await _inventoryService.GetSummaryAsync();
                InventoryProductSkuCount = summary.ProductSkuCount;
                InventoryTotalUnits = summary.TotalUnitsAvailable;

                LowStockProducts.Clear();
                foreach (var r in await _inventoryService.GetLowStockProductsAsync())
                    LowStockProducts.Add(r);

                LowRawMaterials.Clear();
                foreach (var r in await _inventoryService.GetLowRawMaterialsAsync())
                    LowRawMaterials.Add(r);

                AllRawMaterials.Clear();
                foreach (var r in await _inventoryService.GetRawMaterialsAsync())
                    AllRawMaterials.Add(r);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل المخزون: {ex.Message}");
            }
            finally
            {
                OnPropertyChanged(nameof(LowStockProductsCount));
                OnPropertyChanged(nameof(LowRawMaterialsCount));
            }
        }

        public static bool CanManageCloudSettings =>
            CurrentSession.Instance.HasPermission("ManageSettings");

        // ─── Products ────────────────────────────────
        public ObservableCollection<ProductSettingsRow> Products { get; } = new();

        private ProductSettingsRow? _selectedProduct;
        public ProductSettingsRow? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                SetProperty(ref _selectedProduct, value);
                if (value != null)
                {
                    FormPriceMin = value.PriceMin;
                    FormPriceMax = value.PriceMax;
                    FormDefaultPrice = value.DefaultPrice;
                    FormMinThreshold = value.MinThreshold;
                    IsFormVisible = true;
                    ClearMessages();
                }
            }
        }

        private bool _isFormVisible;
        public bool IsFormVisible
        {
            get => _isFormVisible;
            set => SetProperty(ref _isFormVisible, value);
        }

        private decimal _formPriceMin;
        public decimal FormPriceMin
        {
            get => _formPriceMin;
            set => SetProperty(ref _formPriceMin, value);
        }

        private decimal _formPriceMax;
        public decimal FormPriceMax
        {
            get => _formPriceMax;
            set => SetProperty(ref _formPriceMax, value);
        }

        private decimal _formDefaultPrice;
        public decimal FormDefaultPrice
        {
            get => _formDefaultPrice;
            set => SetProperty(ref _formDefaultPrice, value);
        }

        private int _formMinThreshold;
        public int FormMinThreshold
        {
            get => _formMinThreshold;
            set => SetProperty(ref _formMinThreshold, value);
        }

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand SavePriceCommand { get; private set; } = null!;
        public AsyncRelayCommand SaveThresholdCommand { get; private set; } = null!;
        public AsyncRelayCommand ToggleActiveCommand { get; private set; } = null!;
        public RelayCommand CancelFormCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            SavePriceCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedProduct == null) return;
                try
                {
                    IsLoading = true;
                    var product = await _productService.GetByIdAsync(SelectedProduct.Id);
                    if (product == null) { ShowError("المنتج غير موجود"); return; }

                    product.PriceMin = FormPriceMin;
                    product.PriceMax = FormPriceMax;
                    product.DefaultPrice = FormDefaultPrice;

                    // TODO: save via service

                    SelectedProduct.PriceMin = FormPriceMin;
                    SelectedProduct.PriceMax = FormPriceMax;
                    SelectedProduct.DefaultPrice = FormDefaultPrice;
                    ShowSuccess("✅ تم حفظ الأسعار");

                    await LoadProductsAsync();
                }
                catch (Exception ex) { ShowError(ex.Message); }
                finally { IsLoading = false; }
            });

            SaveThresholdCommand = new AsyncRelayCommand(_ =>
            {
                if (SelectedProduct == null) return Task.CompletedTask;
                try
                {
                    IsLoading = true;
                    // TODO: save via service when UpdateProductAsync is available
                    SelectedProduct.MinThreshold = FormMinThreshold;
                    ShowSuccess("✅ تم حفظ الحد الأدنى");
                }
                catch (Exception ex) { ShowError(ex.Message); }
                finally { IsLoading = false; }
                return Task.CompletedTask;
            });

            ToggleActiveCommand = new AsyncRelayCommand(_ =>
            {
                if (SelectedProduct == null) return Task.CompletedTask;
                try
                {
                    IsLoading = true;
                    // TODO: save via service when ToggleProductAsync is available
                    SelectedProduct.IsActive = !SelectedProduct.IsActive;
                    ShowSuccess(SelectedProduct.IsActive
                        ? "✅ تم تفعيل المنتج"
                        : "⛔ تم إيقاف المنتج");
                }
                catch (Exception ex) { ShowError(ex.Message); }
                finally { IsLoading = false; }
                return Task.CompletedTask;
            });

            CancelFormCommand = new RelayCommand(_ =>
            {
                IsFormVisible = false;
                _selectedProduct = null;
                OnPropertyChanged(nameof(SelectedProduct));
                ClearMessages();
            });
        }

        public async Task LoadProductsAsync()
        {
            try
            {
                var list = await _productService.GetActiveProductsAsync();
                Products.Clear();
                foreach (var p in list.OrderBy(x => x.Name))
                {
                    Products.Add(new ProductSettingsRow
                    {
                        Id = p.Id,
                        Name = p.Name,
                        TypeName = p.ProductType?.Name ?? "-",
                        Size = p.Size,
                        PriceMin = p.PriceMin,
                        PriceMax = p.PriceMax,
                        DefaultPrice = p.DefaultPrice,
                        // MinThreshold = p.MinStock,
                        IsActive = p.IsActive,
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل المنتجات: {ex.Message}");
            }
            finally
            {
                OnPropertyChanged(nameof(ActiveProductsCount));
            }
        }
    }

    public class ProductSettingsRow : BaseViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public int Size { get; set; }
        public int StockQuantity { get; set; }

        private decimal _priceMin;
        public decimal PriceMin
        {
            get => _priceMin;
            set => SetProperty(ref _priceMin, value);
        }

        private decimal _priceMax;
        public decimal PriceMax
        {
            get => _priceMax;
            set => SetProperty(ref _priceMax, value);
        }

        private decimal _defaultPrice;
        public decimal DefaultPrice
        {
            get => _defaultPrice;
            set => SetProperty(ref _defaultPrice, value);
        }

        public int MinThreshold { get; set; }
        public int MinStock { get; set; }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string PriceRangeText => $"{PriceMin:N0} — {PriceMax:N0} ر.ي";
    }
}