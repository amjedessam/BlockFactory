using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Models.Inventory;

namespace BlockFactory.Core.Interfaces.Repositories
{
    public interface IInventoryRepository : IRepository<InventoryStock>
    {
        Task<InventoryStock?> GetByProductAsync(int productId);
        Task<IEnumerable<InventoryStock>> GetLowStockAsync();
        Task<IEnumerable<RawMaterial>> GetLowRawMaterialsAsync();
        Task UpdateStockAsync(int productId, int quantity,
            TransactionType type, string? reference = null);
    }
}
