using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Finance
{
    public enum ExpenseCategory
    {
        Electricity,    // كهرباء
        Maintenance,    // صيانة
        Fuel,           // وقود
        Transport,      // نقل
        Stationary,     // قرطاسية
        Other           // أخرى
    }

    public class Expense : BaseEntity
    {
        public ExpenseCategory Category { get; set; }
        public string? CategoryOther { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public bool IsRecurring { get; set; } = false;
        // هل هو مصروف دوري (الكهرباء مثلاً)

        public string? Notes { get; set; }

        // FK — مرتبط بحساب المصروفات
        public int? AccountId { get; set; }
        public Account? Account { get; set; }
    }
}
