
using BlockFactory.Core.Models.Auth;
using BlockFactory.Core.Models.Finance;
using BlockFactory.Core.Models.Inventory;
using BlockFactory.Core.Models.Products;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Data.Seeders
{
    public static partial class DatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // الترتيب مهم جداً — لا تغيّره
            await SeedRolesAsync(context);
            await SeedUsersAsync(context);
            await SeedProductTypesAsync(context);
            await SeedProductsAsync(context);
            await SeedRawMaterialsAsync(context);
            await SeedAccountsAsync(context);
            await SeedElectronicWalletsAsync(context);
            await SeedInventoryStocksAsync(context);

            await context.SaveChangesAsync();
        }
    }
}
