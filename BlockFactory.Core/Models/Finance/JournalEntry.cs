using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Finance
{
    public enum JournalEntryType
    {
        Sale,               // بيع
        Purchase,           // شراء
        Payment,            // دفع
        Receipt,            // استلام
        Salary,             // راتب
        Expense,            // مصروف
        Adjustment,         // تسوية
        Opening             // قيد افتتاحي
    }

    public class JournalEntry : BaseEntity
    {
        public string EntryNumber { get; set; } = string.Empty;
        // JRN-2024-0001

        public DateTime EntryDate { get; set; } = DateTime.Now;
        public JournalEntryType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public bool IsPosted { get; set; } = false;
        // هل تم ترحيله للحسابات

        public string? Reference { get; set; }
        // رقم الفاتورة أو الطلب المرتبط

        // Navigation
        public ICollection<JournalEntryLine> Lines { get; set; }
            = new List<JournalEntryLine>();
    }
}
