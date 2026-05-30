using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockFactory.Core.DTOs.Inventory;
using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Core.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _uow;

        public InventoryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<InventorySummaryDto> GetSummaryAsync()
        {
            var stocks = await _uow.Inventory.Query()
                .Include(s => s.Product)
                .Where(s => !s.IsDeleted)
                .ToListAsync();

            var lowProducts = await _uow.Inventory.GetLowStockAsync();
            var lowMaterials = await _uow.Inventory.GetLowRawMaterialsAsync();

            return new InventorySummaryDto
            {
                ProductSkuCount = stocks.Count,
                TotalUnitsAvailable = stocks.Sum(s => s.QuantityAvailable),
                LowStockProductCount = lowProducts.Count(),
                LowRawMaterialCount = lowMaterials.Count()
            };
        }

        public async Task<IEnumerable<InventoryProductRowDto>>
            GetLowStockProductsAsync()
        {
            var rows = await _uow.Inventory.GetLowStockAsync();
            return rows.Select(s => new InventoryProductRowDto
            {
                ProductId = s.ProductId,
                ProductName = s.Product?.Name ?? "-",
                QuantityAvailable = s.QuantityAvailable,
                MinimumThreshold = s.MinimumThreshold
            });
        }

        public async Task<IEnumerable<InventoryMaterialRowDto>>
                    GetLowRawMaterialsAsync()
        {
            var rows = await _uow.Inventory.GetLowRawMaterialsAsync();
            return rows.Select(m => new InventoryMaterialRowDto
            {
                RawMaterialId = m.Id,
                Name = m.Name,
                QuantityAvailable = m.QuantityAvailable,
                MinimumThreshold = m.MinimumThreshold
            });
        }

        public async Task<IEnumerable<InventoryMaterialRowDto>> GetRawMaterialsAsync()
        {
            var rows = await _uow.RawMaterials.Query()
                .Where(r => r.IsActive)
                .ToListAsync();

            return rows.Select(m => new InventoryMaterialRowDto
            {
                RawMaterialId = m.Id,
                Name = m.Name,
                QuantityAvailable = m.QuantityAvailable,
                MinimumThreshold = m.MinimumThreshold
            });
        }

    }
}
