
using BlockFactory.Core.Models.Auth;
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
        private static async Task SeedRolesAsync(AppDbContext context)
        {
            if (await context.Roles.AnyAsync()) return;

            var roles = new List<Role>
            {
                new Role
                {
                 //   Id = 1,
                    Name = "Admin",
                    CreatedAt = DateTime.Now
                },
                new Role
                {
                  //  Id = 2,
                    Name = "Accountant",
                    CreatedAt = DateTime.Now
                }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        private static async Task SeedUsersAsync(AppDbContext context)
        {
            if (await context.Users.AnyAsync()) return;

            // جلب الـ Role من DB بعد ما اتحفظ
            var adminRole = await context.Roles
                .FirstAsync(r => r.Name == "Admin");

            var users = new List<User>
    {
        new User
        {
            FullName     = "المدير العام",
            Username     = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            RoleId       = adminRole.Id,  // ← ID حقيقي من DB
            IsActive     = true,
            CreatedAt    = DateTime.Now
        }
    };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }
        /* private static async Task SeedUsersAsync(AppDbContext context)
         {
             if (await context.Users.AnyAsync()) return;

             var users = new List<User>
             {
                 new User
                 {
                    Id = 1,
                     FullName = "المدير العام",
                     Username = "admin",
                     // تشفير كلمة المرور — سنكمل في Auth Service
                     PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    RoleId = 1,
                     IsActive = true,
                     CreatedAt = DateTime.Now
                 }
             };

             await context.Users.AddRangeAsync(users);
             await context.SaveChangesAsync();
         }*/
    }
}
