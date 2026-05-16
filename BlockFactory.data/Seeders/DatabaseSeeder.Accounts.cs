
using BlockFactory.Core.Models.Finance;
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
        // احذف كل Id=1, Id=2... من الـ Accounts
        // لكن ParentAccountId مشكلة — الحسابات الفرعية تحتاج ID الحساب الأب
        // الحل: أضف الحسابات الرئيسية أولاً ثم الفرعية
        private static async Task SeedAccountsAsync(AppDbContext context)
        {
            if (await context.Accounts.AnyAsync()) return;

            // الحسابات الرئيسية أولاً
            var mainAccounts = new List<Account>
    {
        new Account { Code="1000", Name="الأصول",        Type=AccountType.Asset,     IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="2000", Name="الخصوم",        Type=AccountType.Liability, IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="3000", Name="حقوق الملكية",  Type=AccountType.Equity,    IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="4000", Name="الإيرادات",     Type=AccountType.Revenue,   IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="5000", Name="المصروفات",     Type=AccountType.Expense,   IsSystem=true, CreatedAt=DateTime.Now },
    };
            await context.Accounts.AddRangeAsync(mainAccounts);
            await context.SaveChangesAsync();

            // جلب IDs الحقيقية
            var assets = await context.Accounts.FirstAsync(a => a.Code == "1000");
            var liability = await context.Accounts.FirstAsync(a => a.Code == "2000");
            var equity = await context.Accounts.FirstAsync(a => a.Code == "3000");
            var revenue = await context.Accounts.FirstAsync(a => a.Code == "4000");
            var expense = await context.Accounts.FirstAsync(a => a.Code == "5000");

            // الحسابات الفرعية
            var subAccounts = new List<Account>
    {
        new Account { Code="1001", Name="الصندوق النقدي",       Type=AccountType.Asset,     ParentAccountId=assets.Id,    IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="1002", Name="المحافظ الإلكترونية",  Type=AccountType.Asset,     ParentAccountId=assets.Id,    IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="1003", Name="ذمم العملاء",          Type=AccountType.Asset,     ParentAccountId=assets.Id,    IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="1004", Name="مخزون البلوك",         Type=AccountType.Asset,     ParentAccountId=assets.Id,    IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="1005", Name="مخزون المواد الخام",   Type=AccountType.Asset,     ParentAccountId=assets.Id,    IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="2001", Name="ذمم الموردين",         Type=AccountType.Liability, ParentAccountId=liability.Id, IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="2002", Name="رواتب مستحقة",         Type=AccountType.Liability, ParentAccountId=liability.Id, IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="3001", Name="رأس المال",            Type=AccountType.Equity,    ParentAccountId=equity.Id,    IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="4001", Name="إيرادات المبيعات",     Type=AccountType.Revenue,   ParentAccountId=revenue.Id,   IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="4002", Name="إيرادات التوصيل",      Type=AccountType.Revenue,   ParentAccountId=revenue.Id,   IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="5001", Name="تكلفة المواد الخام",   Type=AccountType.Expense,   ParentAccountId=expense.Id,   IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="5002", Name="رواتب العمال",         Type=AccountType.Expense,   ParentAccountId=expense.Id,   IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="5003", Name="مصروف الكهرباء",      Type=AccountType.Expense,   ParentAccountId=expense.Id,   IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="5004", Name="مصروف الصيانة",       Type=AccountType.Expense,   ParentAccountId=expense.Id,   IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="5005", Name="مصروف النقل",         Type=AccountType.Expense,   ParentAccountId=expense.Id,   IsSystem=true, CreatedAt=DateTime.Now },
        new Account { Code="5006", Name="مصروفات أخرى",        Type=AccountType.Expense,   ParentAccountId=expense.Id,   IsSystem=true, CreatedAt=DateTime.Now },
    };
            await context.Accounts.AddRangeAsync(subAccounts);
            await context.SaveChangesAsync();
        }


        /* private static async Task SeedAccountsAsync(AppDbContext context)
         {
             if (await context.Accounts.AnyAsync()) return;

             var accounts = new List<Account>
             {
                 // ═══ الأصول ══════════════════════════
                 new Account {
                //     Id=1,
                    Code="1000",
                     Name="الأصول", Type=AccountType.Asset,
                     IsSystem=true, CreatedAt=DateTime.Now },

                 new Account { Id=2, Code="1001",
                     Name="الصندوق النقدي", Type=AccountType.Asset,
                     ParentAccountId=1, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 new Account { Id=3, Code="1002",
                     Name="المحافظ الإلكترونية", Type=AccountType.Asset,
                     ParentAccountId=1, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 new Account { Id=4, Code="1003",
                     Name="ذمم العملاء", Type=AccountType.Asset,
                     ParentAccountId=1, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 new Account { Id=5, Code="1004",
                     Name="مخزون البلوك", Type=AccountType.Asset,
                     ParentAccountId=1, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 new Account { Id=6, Code="1005",
                     Name="مخزون المواد الخام", Type=AccountType.Asset,
                     ParentAccountId=1, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 // ═══ الخصوم ══════════════════════════
                 new Account { Id=7, Code="2000",
                     Name="الخصوم", Type=AccountType.Liability,
                     IsSystem=true, CreatedAt=DateTime.Now },

                 new Account { Id=8, Code="2001",
                     Name="ذمم الموردين", Type=AccountType.Liability,
                     ParentAccountId=7, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 new Account { Id=9, Code="2002",
                     Name="رواتب مستحقة", Type=AccountType.Liability,
                     ParentAccountId=7, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 // ═══ حقوق الملكية ════════════════════
                 new Account { Id=10, Code="3000",
                     Name="حقوق الملكية", Type=AccountType.Equity,
                     IsSystem=true, CreatedAt=DateTime.Now },

                 new Account { Id=11, Code="3001",
                     Name="رأس المال", Type=AccountType.Equity,
                     ParentAccountId=10, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 // ═══ الإيرادات ═══════════════════════
                 new Account { Id=12, Code="4000",
                     Name="الإيرادات", Type=AccountType.Revenue,
                     IsSystem=true, CreatedAt=DateTime.Now },

                 new Account { Id=13, Code="4001",
                     Name="إيرادات المبيعات", Type=AccountType.Revenue,
                     ParentAccountId=12, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 new Account { Id=14, Code="4002",
                     Name="إيرادات التوصيل", Type=AccountType.Revenue,
                     ParentAccountId=12, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 // ═══ المصروفات ═══════════════════════
                 new Account { Id=15, Code="5000",
                     Name="المصروفات", Type=AccountType.Expense,
                     IsSystem=true, CreatedAt=DateTime.Now },

                 new Account { Id=16, Code="5001",
                     Name="تكلفة المواد الخام", Type=AccountType.Expense,
                     ParentAccountId=15, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 new Account { Id=17, Code="5002",
                     Name="رواتب العمال", Type=AccountType.Expense,
                     ParentAccountId=15, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 new Account { Id=18, Code="5003",
                     Name="مصروف الكهرباء", Type=AccountType.Expense,
                     ParentAccountId=15, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 new Account { Id=19, Code="5004",
                     Name="مصروف الصيانة", Type=AccountType.Expense,
                     ParentAccountId=15, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 new Account { Id=20, Code="5005",
                     Name="مصروف النقل", Type=AccountType.Expense,
                     ParentAccountId=15, IsSystem=true,
                     CreatedAt=DateTime.Now },

                 new Account { Id=21, Code="5006",
                     Name="مصروفات أخرى", Type=AccountType.Expense,
                     ParentAccountId=15, IsSystem=true,
                     CreatedAt=DateTime.Now }
             };

             await context.Accounts.AddRangeAsync(accounts);
             await context.SaveChangesAsync();
         }*/
    }
}
