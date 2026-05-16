
using BlockFactory.Core.Models.Finance;
using BlockFactory.Core.Models.Inventory;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Data.Seeders
{
    public static partial class DatabaseSeeder
    {
        private static async Task SeedElectronicWalletsAsync(AppDbContext context)
        {
            if (await context.ElectronicWallets.AnyAsync()) return;

            var wallets = new List<ElectronicWallet>
            {
                new ElectronicWallet
                {
                   // Id = 1,
                    Name = "كاش",
                    Balance = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new ElectronicWallet
                {
                  //  Id = 2,
                    Name = "سبأفون",
                    Balance = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new ElectronicWallet
                {
               //     Id = 3,
                    Name = "وان كاش",
                    Balance = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            };

            await context.ElectronicWallets.AddRangeAsync(wallets);
            await context.SaveChangesAsync();
        }

        private static async Task SeedInventoryStocksAsync(AppDbContext context)
        {
            if (await context.InventoryStocks.AnyAsync()) return;

            // إنشاء سجل مخزون لكل منتج تلقائياً
            var productIds = await context.Products
                .Select(p => p.Id)
                .ToListAsync();

            var stocks = productIds.Select(pid => new InventoryStock
            {
                ProductId = pid,
                QuantityAvailable = 0,
                MinimumThreshold = 100,
                LastUpdated = DateTime.Now,
                CreatedAt = DateTime.Now
            }).ToList();

            await context.InventoryStocks.AddRangeAsync(stocks);
            await context.SaveChangesAsync();
        }
    }
}
