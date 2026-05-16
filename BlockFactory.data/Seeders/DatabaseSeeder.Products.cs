
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Products;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Data.Seeders
{
    public static partial class DatabaseSeeder
    {
        private static async Task SeedProductTypesAsync(AppDbContext context)
        {
            if (await context.ProductTypes.AnyAsync()) return;

            var types = new List<ProductType>
            {
                new ProductType { Name = "بلك صم",         CreatedAt = DateTime.Now },
                new ProductType { Name = "بلك مخرق ثقيل", CreatedAt = DateTime.Now },
                new ProductType { Name = "بلك مخرق خفيف", CreatedAt = DateTime.Now },
                new ProductType { Name = "بلك هردي",       CreatedAt = DateTime.Now }
            };

            await context.ProductTypes.AddRangeAsync(types);
            await context.SaveChangesAsync();
        }

        private static async Task SeedProductsAsync(AppDbContext context)
        {
            if (await context.Products.AnyAsync()) return;

            var solid = await context.ProductTypes.FirstAsync(t => t.Name == "بلك صم");
            var heavyHollow = await context.ProductTypes.FirstAsync(t => t.Name == "بلك مخرق ثقيل");
            var lightHollow = await context.ProductTypes.FirstAsync(t => t.Name == "بلك مخرق خفيف");
            var hardi = await context.ProductTypes.FirstAsync(t => t.Name == "بلك هردي");

            var products = new List<Product>
            {
                // بلك صم
                new Product { Name="بلك صم أبو 10", Size=10, ProductTypeId=solid.Id, PriceMin=350, PriceMax=360, DefaultPrice=350, IsActive=true, CreatedAt=DateTime.Now },
                new Product { Name="بلك صم أبو 15", Size=15, ProductTypeId=solid.Id, PriceMin=400, PriceMax=410, DefaultPrice=400, IsActive=true, CreatedAt=DateTime.Now },
                new Product { Name="بلك صم أبو 20", Size=20, ProductTypeId=solid.Id, PriceMin=500, PriceMax=510, DefaultPrice=500, IsActive=true, CreatedAt=DateTime.Now },

                // بلك مخرق ثقيل
                new Product { Name="بلك مخرق ثقيل أبو 10", Size=10, ProductTypeId=heavyHollow.Id, PriceMin=300, PriceMax=310, DefaultPrice=300, IsActive=true, CreatedAt=DateTime.Now },
                new Product { Name="بلك مخرق ثقيل أبو 15", Size=15, ProductTypeId=heavyHollow.Id, PriceMin=350, PriceMax=360, DefaultPrice=350, IsActive=true, CreatedAt=DateTime.Now },
                new Product { Name="بلك مخرق ثقيل أبو 20", Size=20, ProductTypeId=heavyHollow.Id, PriceMin=400, PriceMax=410, DefaultPrice=400, IsActive=true, CreatedAt=DateTime.Now },

                // بلك مخرق خفيف
                new Product { Name="بلك مخرق خفيف أبو 10", Size=10, ProductTypeId=lightHollow.Id, PriceMin=230, PriceMax=240, DefaultPrice=230, IsActive=true, CreatedAt=DateTime.Now },
                new Product { Name="بلك مخرق خفيف أبو 15", Size=15, ProductTypeId=lightHollow.Id, PriceMin=260, PriceMax=270, DefaultPrice=260, IsActive=true, CreatedAt=DateTime.Now },
                new Product { Name="بلك مخرق خفيف أبو 20", Size=20, ProductTypeId=lightHollow.Id, PriceMin=280, PriceMax=290, DefaultPrice=280, IsActive=true, CreatedAt=DateTime.Now },

                // بلك هردي
                new Product { Name="بلك هردي أبو 15", Size=15, ProductTypeId=hardi.Id, PriceMin=230, PriceMax=240, DefaultPrice=230, IsActive=true, CreatedAt=DateTime.Now },
                new Product { Name="بلك هردي أبو 20", Size=20, ProductTypeId=hardi.Id, PriceMin=250, PriceMax=260, DefaultPrice=250, IsActive=true, CreatedAt=DateTime.Now },
                new Product { Name="بلك هردي أبو 25", Size=25, ProductTypeId=hardi.Id, PriceMin=390, PriceMax=400, DefaultPrice=390, IsActive=true, CreatedAt=DateTime.Now },
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
    }
}
/*
namespace BlockFactory.Data.Seeders
{
    public static partial class DatabaseSeeder
    {
        private static async Task SeedProductTypesAsync(AppDbContext context)
        {
            if (await context.ProductTypes.AnyAsync()) return;

            var types = new List<ProductType>
            {
                new ProductType { Id = 1, Name = "بلك صم",
                    CreatedAt = DateTime.Now },
                new ProductType { Id = 2, Name = "بلك مخرق ثقيل",
                    CreatedAt = DateTime.Now },
                new ProductType { Id = 3, Name = "بلك مخرق خفيف",
                    CreatedAt = DateTime.Now },
                new ProductType { Id = 4, Name = "بلك هردي",
                    CreatedAt = DateTime.Now }
            };

            await context.ProductTypes.AddRangeAsync(types);
            await context.SaveChangesAsync();
        }
        private static async Task SeedProductsAsync(AppDbContext context)
        {
            if (await context.Products.AnyAsync()) return;

            // جلب أنواع البلوك الحقيقية من DB
            var solid = await context.ProductTypes.FirstAsync(t => t.Name == "بلك صم");
            var heavyHollow = await context.ProductTypes.FirstAsync(t => t.Name == "بلك مخرق ثقيل");
            var lightHollow = await context.ProductTypes.FirstAsync(t => t.Name == "بلك مخرق خفيف");
            var hardi = await context.ProductTypes.FirstAsync(t => t.Name == "بلك هردي");

            var products = new List<Product>
    {
        // بلك صم
        new Product { Name="بلك صم أبو 10", Size=10, ProductTypeId=solid.Id,       PriceMin=350, PriceMax=360, DefaultPrice=350, IsActive=true, CreatedAt=DateTime.Now },
        new Product { Name="بلك صم أبو 15", Size=15, ProductTypeId=solid.Id,       PriceMin=400, PriceMax=410, DefaultPrice=400, IsActive=true, CreatedAt=DateTime.Now },
        new Product { Name="بلك صم أبو 20", Size=20, ProductTypeId=solid.Id,       PriceMin=500, PriceMax=510, DefaultPrice=500, IsActive=true, CreatedAt=DateTime.Now },
        // بلك مخرق ثقيل
        new Product { Name="بلك مخرق ثقيل أبو 10", Size=10, ProductTypeId=heavyHollow.Id, PriceMin=300, PriceMax=310, DefaultPrice=300, IsActive=true, CreatedAt=DateTime.Now },
        new Product { Name="بلك مخرق ثقيل أبو 15", Size=15, ProductTypeId=heavyHollow.Id, PriceMin=350, PriceMax=360, DefaultPrice=350, IsActive=true, CreatedAt=DateTime.Now },
        new Product { Name="بلك مخرق ثقيل أبو 20", Size=20, ProductTypeId=heavyHollow.Id, PriceMin=400, PriceMax=410, DefaultPrice=400, IsActive=true, CreatedAt=DateTime.Now },
        // بلك مخرق خفيف
        new Product { Name="بلك مخرق خفيف أبو 10", Size=10, ProductTypeId=lightHollow.Id, PriceMin=230, PriceMax=240, DefaultPrice=230, IsActive=true, CreatedAt=DateTime.Now },
        new Product { Name="بلك مخرق خفيف أبو 15", Size=15, ProductTypeId=lightHollow.Id, PriceMin=260, PriceMax=270, DefaultPrice=260, IsActive=true, CreatedAt=DateTime.Now },
        new Product { Name="بلك مخرق خفيف أبو 20", Size=20, ProductTypeId=lightHollow.Id, PriceMin=280, PriceMax=290, DefaultPrice=280, IsActive=true, CreatedAt=DateTime.Now },
        // بلك هردي
        new Product { Name="بلك هردي أبو 15", Size=15, ProductTypeId=hardi.Id, PriceMin=230, PriceMax=240, DefaultPrice=230, IsActive=true, CreatedAt=DateTime.Now },
        new Product { Name="بلك هردي أبو 20", Size=20, ProductTypeId=hardi.Id, PriceMin=250, PriceMax=260, DefaultPrice=250, IsActive=true, CreatedAt=DateTime.Now },
        new Product { Name="بلك هردي أبو 25", Size=25, ProductTypeId=hardi.Id, PriceMin=390, PriceMax=400, DefaultPrice=390, IsActive=true, CreatedAt=DateTime.Now },
    };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
        /*   private static async Task SeedProductsAsync(AppDbContext context)
           {
               if (await context.Products.AnyAsync()) return;

               var products = new List<Product>
               {
                   // ─── بلك صم ───────────────────────────
                   new Product
                   {
                       Id = 1,
                       Name = "بلك صم أبو 10",
                       Size = 10,
                       ProductTypeId = 1,
                       PriceMin = 350,
                       PriceMax = 360,
                       DefaultPrice = 350,
                       IsActive = true,
                       CreatedAt = DateTime.Now
                   },
                   new Product
                   {
                       Id = 2,
                       Name = "بلك صم أبو 15",
                       Size = 15,
                       ProductTypeId = 1,
                       PriceMin = 400,
                       PriceMax = 410,
                       DefaultPrice = 400,
                       IsActive = true,
                       CreatedAt = DateTime.Now
                   },
                   new Product
                   {
                       Id = 3,
                       Name = "بلك صم أبو 20",
                       Size = 20,
                       ProductTypeId = 1,
                       PriceMin = 500,
                       PriceMax = 510,
                       DefaultPrice = 500,
                       IsActive = true,
                       CreatedAt = DateTime.Now
                   },

                   // ─── بلك مخرق ثقيل ───────────────────
                   new Product
                   {
                       Id = 4,
                       Name = "بلك مخرق ثقيل أبو 10",
                       Size = 10,
                       ProductTypeId = 2,
                       PriceMin = 300,
                       PriceMax = 310,
                       DefaultPrice = 300,
                       IsActive = true,
                       CreatedAt = DateTime.Now
                   },
                   new Product
                   {
                       Id = 5,
                       Name = "بلك مخرق ثقيل أبو 15",
                       Size = 15,
                       ProductTypeId = 2,
                       PriceMin = 350,
                       PriceMax = 360,
                       DefaultPrice = 350,
                       IsActive = true,
                       CreatedAt = DateTime.Now
                   },
                   new Product
                   {
                       Id = 6,
                       Name = "بلك مخرق ثقيل أبو 20",
                       Size = 20,
                       ProductTypeId = 2,
                       PriceMin = 400,
                       PriceMax = 410,
                       DefaultPrice = 400,
                       IsActive = true,
                       CreatedAt = DateTime.Now
                   },

                   // ─── بلك مخرق خفيف ───────────────────
                   new Product
                   {
                       Id = 7,
                       Name = "بلك مخرق خفيف أبو 10",
                       Size = 10,
                       ProductTypeId = 3,
                       PriceMin = 230,
                       PriceMax = 240,
                       DefaultPrice = 230,
                       IsActive = true,
                       CreatedAt = DateTime.Now
                   },
                   new Product
                   {
                       Id = 8,
                       Name = "بلك مخرق خفيف أبو 15",
                       Size = 15,
                       ProductTypeId = 3,
                       PriceMin = 260,
                       PriceMax = 270,
                       DefaultPrice = 260,
                       IsActive = true,
                       CreatedAt = DateTime.Now
                   },
                   new Product
                   {
                       Id = 9,
                       Name = "بلك مخرق خفيف أبو 20",
                       Size = 20,
                       ProductTypeId = 3,
                       PriceMin = 280,
                       PriceMax = 290,
                       DefaultPrice = 280,
                       IsActive = true,
                       CreatedAt = DateTime.Now
                   },

                   // ─── بلك هردي ────────────────────────
                   new Product
                   {
                       Id = 10,
                       Name = "بلك هردي أبو 15",
                       Size = 15,
                       ProductTypeId = 4,
                       PriceMin = 230,
                       PriceMax = 240,
                       DefaultPrice = 230,
                       IsActive = true,
                       CreatedAt = DateTime.Now
                   },
                   new Product
                   {
                       Id = 11,
                       Name = "بلك هردي أبو 20",
                       Size = 20,
                       ProductTypeId = 4,
                       PriceMin = 250,
                       PriceMax = 260,
                       DefaultPrice = 250,
                       IsActive = true,
                       CreatedAt = DateTime.Now
                   },
                   new Product
                   {
                       Id = 12,
                       Name = "بلك هردي أبو 25",
                       Size = 25,
                       ProductTypeId = 4,
                       PriceMin = 390,
                       PriceMax = 400,
                       DefaultPrice = 390,
                       IsActive = true,
                       CreatedAt = DateTime.Now
                   }
               };

               await context.Products.AddRangeAsync(products);
               await context.SaveChangesAsync();
           }*/
    