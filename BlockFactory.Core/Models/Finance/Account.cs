using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Finance
{
    public enum AccountType
    {
        Asset,      // أصول — الصندوق، البنك، المخزون
        Liability,  // خصوم — الديون للموردين
        Equity,     // حقوق الملكية
        Revenue,    // إيرادات — المبيعات
        Expense     // مصروفات — رواتب، كهرباء
    }

    public class Account : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        // 1001, 2001, 4001...

        public string Name { get; set; } = string.Empty;
        // الصندوق، كاش، سبأفون، مبيعات...

        public AccountType Type { get; set; }
        public decimal Balance { get; set; } = 0;
        public bool IsSystem { get; set; } = false;
        // الحسابات الأساسية لا تُحذف

        public bool IsActive { get; set; } = true;

        public int? ParentAccountId { get; set; }
        public Account? ParentAccount { get; set; }
        // للحسابات الفرعية

        public ICollection<Account> SubAccounts { get; set; }
            = new List<Account>();

        public ICollection<JournalEntryLine> JournalLines { get; set; }
            = new List<JournalEntryLine>();
    }
}
