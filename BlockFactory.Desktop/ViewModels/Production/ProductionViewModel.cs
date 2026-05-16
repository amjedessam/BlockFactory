using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.DTOs.Production;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Production;
using BlockFactory.Core.Session;
using BlockFactory.Desktop.Commands;
using BlockFactory.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;
using BlockFactory.Desktop.Mappers;
using BlockFactory.Desktop.ViewModels.Orders;

namespace BlockFactory.Desktop.ViewModels.Production
{
    public class ProductionViewModel : BaseViewModel
    {
        private readonly IProductionService _productionService;
        private readonly IProductService _productService;

        public ProductionViewModel(
            IProductionService productionService,
            IProductService productService)
        {
            _productionService = productionService;
            _productService = productService;
            InitializeCommands();
        }

        public bool CanDeleteProduction =>
            CurrentSession.Instance.HasPermission("DeleteProduction");

        // ─── Daily Summary ───────────────────────────
        private DailyProductionSummaryDto? _dailySummary;
        public DailyProductionSummaryDto? DailySummary
        {
            get => _dailySummary;
            set => SetProperty(ref _dailySummary, value);
        }

        // ─── Stats ───────────────────────────────────
        private ProductionStatsDto? _stats;
        public ProductionStatsDto? Stats
        {
            get => _stats;
            set => SetProperty(ref _stats, value);
        }

        // ─── Collections ────────────────────────────
        public ObservableCollection<ProductionRecordListDto> Records { get; }
            = new();

        public ObservableCollection<ProductLookupDto> Products { get; }
            = new();

        public ObservableCollection<MaterialUsageFormDto> MaterialUsages { get; }
            = new();

        // ─── Form Properties ─────────────────────────
        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                SetProperty(ref _selectedDate, value);
                _ = LoadDailySummaryAsync();
            }
        }

        private bool _isFormVisible;
        public bool IsFormVisible
        {
            get => _isFormVisible;
            set => SetProperty(ref _isFormVisible, value);
        }

        private ProductLookupDto? _selectedProduct;
        public ProductLookupDto? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                SetProperty(ref _selectedProduct, value);
                if (value != null)
                    _ = LoadFormulaAsync(value.Id);
            }
        }

        private ProductionShift _selectedShift = ProductionShift.Morning;
        public ProductionShift SelectedShift
        {
            get => _selectedShift;
            set => SetProperty(ref _selectedShift, value);
        }

        private int _quantityProduced;
        public int QuantityProduced
        {
            get => _quantityProduced;
            set
            {
                SetProperty(ref _quantityProduced, value);
                OnPropertyChanged(nameof(QuantityNet));
            }
        }

        private int _quantityDefective;
        public int QuantityDefective
        {
            get => _quantityDefective;
            set
            {
                SetProperty(ref _quantityDefective, value);
                OnPropertyChanged(nameof(QuantityNet));
            }
        }

        public int QuantityNet => QuantityProduced - QuantityDefective;

        private string? _notes;
        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        // خيارات الوردية
        public List<ShiftItem> Shifts { get; } = new()
        {
            new("صباحي", ProductionShift.Morning),
            new("مسائي", ProductionShift.Evening)
        };

        // ─── Selected Record ─────────────────────────
        private ProductionRecordListDto? _selectedRecord;
        public ProductionRecordListDto? SelectedRecord
        {
            get => _selectedRecord;
            set => SetProperty(ref _selectedRecord, value);
        }

        // ─── Commands ───────────────────────────────
        public AsyncRelayCommand LoadCommand { get; private set; } = null!;
        public RelayCommand ShowFormCommand { get; private set; } = null!;
        public RelayCommand HideFormCommand { get; private set; } = null!;
        public AsyncRelayCommand SaveCommand { get; private set; } = null!;
        public AsyncRelayCommand DeleteCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            LoadCommand = new AsyncRelayCommand(
                async _ => await LoadAllAsync());

            ShowFormCommand = new RelayCommand(_ =>
            {
                ClearForm();
                IsFormVisible = true;
            });

            HideFormCommand = new RelayCommand(_ =>
            {
                IsFormVisible = false;
                ClearForm();
            });

            SaveCommand = new AsyncRelayCommand(
                async _ => await SaveAsync(),
                _ => SelectedProduct != null &&
                     QuantityProduced > 0 &&
                     QuantityNet >= 0);

            DeleteCommand = new AsyncRelayCommand(
                async _ => await DeleteAsync(),
                _ => SelectedRecord != null &&
                     CurrentSession.Instance.HasPermission("DeleteProduction"));
        }

        // ─── Load ────────────────────────────────────
        public async Task LoadAllAsync()
        {
            try
            {
                IsLoading = true;

                // تحميل المنتجات
                if (Products.Count == 0)
                {
                    var products = await _productService
                        .GetActiveProductsAsync();
                    foreach (var p in products)
                        Products.Add(ProductLookupMapper.ToLookupDto(p));
                }

                // تحميل الإحصائيات
                Stats = await _productionService.GetStatsAsync();

                // تحميل الملخص اليومي
                await LoadDailySummaryAsync();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadDailySummaryAsync()
        {
            try
            {
                DailySummary = await _productionService
                    .GetDailySummaryAsync(SelectedDate);

                Records.Clear();
                foreach (var r in DailySummary.Records)
                    Records.Add(r);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل الملخص: {ex.Message}");
            }
        }

        private async Task LoadFormulaAsync(int productId)
        {
            try
            {
                var formula = await _productionService
                    .GetFormulaAsync(productId);

                MaterialUsages.Clear();
                foreach (var f in formula)
                {
                    MaterialUsages.Add(new MaterialUsageFormDto
                    {
                        RawMaterialId = f.RawMaterialId,
                        MaterialName = f.MaterialName,
                        QuantityUsed = f.QuantityUsed,
                        Unit = f.Unit
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل الوصفة: {ex.Message}");
            }
        }

        // ─── Save ────────────────────────────────────
        private async Task SaveAsync()
        {
            if (SelectedProduct == null) return;

            try
            {
                IsLoading = true;
                ClearMessages();

                var dto = new CreateProductionDto
                {
                    ProductionDate = SelectedDate,
                    ProductId = SelectedProduct.Id,
                    ProductName = SelectedProduct.Name,
                    Shift = SelectedShift,
                    QuantityProduced = QuantityProduced,
                    QuantityDefective = QuantityDefective,
                    Notes = Notes,
                    MaterialUsages = MaterialUsages
                        .Where(m => m.QuantityUsed > 0)
                        .Select(m => new CreateMaterialUsageDto
                        {
                            RawMaterialId = m.RawMaterialId,
                            MaterialName = m.MaterialName,
                            QuantityUsed = m.QuantityUsed,
                            Unit = m.Unit
                        }).ToList()
                };

                var result = await _productionService
                    .CreateProductionRecordAsync(dto);

                if (result.Success)
                {
                    ShowSuccess(result.Message);
                    IsFormVisible = false;
                    ClearForm();
                    await LoadAllAsync();
                }
                else ShowError(result.Message);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ─── Delete ──────────────────────────────────
        private async Task DeleteAsync()
        {
            if (SelectedRecord == null) return;

            var confirm = MessageBox.Show(
                $"هل تريد حذف سجل إنتاج\n" +
                $"{SelectedRecord.ProductName} — " +
                $"{SelectedRecord.QuantityProduced} قطعة؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);

            if (confirm != MessageBoxResult.Yes) return;

            var result = await _productionService
                .DeleteProductionRecordAsync(SelectedRecord.Id);

            if (result.Success)
            {
                ShowSuccess(result.Message);
                await LoadAllAsync();
            }
            else ShowError(result.Message);
        }

        private void ClearForm()
        {
            SelectedProduct = null;
            SelectedShift = ProductionShift.Morning;
            QuantityProduced = 0;
            QuantityDefective = 0;
            Notes = null;
            MaterialUsages.Clear();
            ClearMessages();
        }
    }

    // ─── Helper Models ───────────────────────────────
    public class MaterialUsageFormDto : BaseViewModel
    {
        public int RawMaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        private decimal _quantityUsed;
        public decimal QuantityUsed
        {
            get => _quantityUsed;
            set => SetProperty(ref _quantityUsed, value);
        }
    }

    public record ShiftItem(string Name, ProductionShift Value);
}
