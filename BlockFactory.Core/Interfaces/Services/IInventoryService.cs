using System.Collections.Generic;
using System.Threading.Tasks;
using BlockFactory.Core.DTOs.Inventory;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface IInventoryService
    {
        Task<InventorySummaryDto> GetSummaryAsync();
        Task<IEnumerable<InventoryProductRowDto>> GetLowStockProductsAsync();
        Task<IEnumerable<InventoryMaterialRowDto>> GetLowRawMaterialsAsync();
        Task<IEnumerable<InventoryMaterialRowDto>> GetRawMaterialsAsync();
    }
}
