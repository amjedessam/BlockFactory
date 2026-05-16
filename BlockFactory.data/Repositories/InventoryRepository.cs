using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Interfaces.Repositories;
using BlockFactory.Core.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Data.Repositories
{
    public class InventoryRepository
        : Repository<InventoryStock>, IInventoryRepository
    {
        public InventoryRepository(AppDbContext context)
            : base(context) { }

        public async Task<InventoryStock?> GetByProductAsync(int productId)
            => await _context.InventoryStocks
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.ProductId == productId);

        public async Task<IEnumerable<InventoryStock>> GetLowStockAsync()
            => await _context.InventoryStocks
                .Include(s => s.Product)
                .Where(s => s.QuantityAvailable <= s.MinimumThreshold)
                .ToListAsync();

        public async Task<IEnumerable<RawMaterial>> GetLowRawMaterialsAsync()
            => await _context.RawMaterials
                .Where(r => r.QuantityAvailable <= r.MinimumThreshold
                         && r.IsActive)
                .ToListAsync();

        public async Task UpdateStockAsync(int productId, int quantity,
            TransactionType type, string? reference = null)
        {
            var stock = await _context.InventoryStocks
                .FirstOrDefaultAsync(s => s.ProductId == productId);

            if (stock == null) return;

            var before = stock.QuantityAvailable;

            stock.QuantityAvailable = type switch
            {
                TransactionType.ProductionIn => stock.QuantityAvailable + quantity,
                TransactionType.SaleOut => stock.QuantityAvailable - quantity,
                TransactionType.AdjustmentIn => stock.QuantityAvailable + quantity,
                TransactionType.AdjustmentOut => stock.QuantityAvailable - quantity,
                TransactionType.ReturnIn => stock.QuantityAvailable + quantity,
                _ => stock.QuantityAvailable
            };

            stock.LastUpdated = DateTime.Now;

            // تسجيل الحركة
            var transaction = new InventoryTransaction
            {
                ProductId = productId,
                Type = type,
                Quantity = quantity,
                QuantityBefore = before,
                QuantityAfter = stock.QuantityAvailable,
                TransactionDate = DateTime.Now,
                Reference = reference
            };

            await _context.InventoryTransactions.AddAsync(transaction);
        }
    }
}