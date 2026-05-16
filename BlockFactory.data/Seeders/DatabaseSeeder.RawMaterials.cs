
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
        private static async Task SeedRawMaterialsAsync(AppDbContext context)
        {
            if (await context.RawMaterials.AnyAsync()) return;

            var materials = new List<RawMaterial>
            {
                new RawMaterial
                {
                  //  Id = 1,
                    Name = "إسمنت",
                    Unit = MaterialUnit.Bag,
                    QuantityAvailable = 0,
                    MinimumThreshold = 50,
                    CurrentUnitCost = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new RawMaterial
                {
                //    Id = 2,
                    Name = "رمل",
                    Unit = MaterialUnit.Ton,
                    QuantityAvailable = 0,
                    MinimumThreshold = 5,
                    CurrentUnitCost = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new RawMaterial
                {
               //     Id = 3,
                    Name = "حصى",
                    Unit = MaterialUnit.Ton,
                    QuantityAvailable = 0,
                    MinimumThreshold = 5,
                    CurrentUnitCost = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new RawMaterial
                {
                //    Id = 4,
                    Name = "ماء",
                    Unit = MaterialUnit.Cubic,
                    QuantityAvailable = 0,
                    MinimumThreshold = 2,
                    CurrentUnitCost = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            };

            await context.RawMaterials.AddRangeAsync(materials);
            await context.SaveChangesAsync();
        }
    }
}
