using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Data
{
    public static class DatabaseConfig
    {
        public static string GetConnectionString()
        {
            // سطر واحد — أسهل للتحليل وأقل عرضة لمشاكل المسافات/أسطر الاستمرارية
            return
                "Server=(localdb)\\MSSQLLocalDB;" +
                "Database=BlockFactoryDB;" +
                "Trusted_Connection=True;" +
                "MultipleActiveResultSets=true;" +
                "TrustServerCertificate=True;" +
                "Connection Timeout=30";
        }

        public static DbContextOptions<AppDbContext> GetOptions()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(
                GetConnectionString(),
                sql => sql.MigrationsAssembly(
                    typeof(AppDbContext).Assembly.GetName().Name!));
            return optionsBuilder.Options;
        }
    }
}
