using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.HR
{
    public enum DeductionType
    {
        Absence,     // غياب
        Lateness,    // تأخير
        Damage,      // تلف
        Other        // أخرى
    }

    public class Deduction : BaseEntity
    {
        public DeductionType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime DeductionDate { get; set; } = DateTime.Now;
        public string Reason { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }

        // FK
        public int WorkerId { get; set; }
        public Worker Worker { get; set; } = null!;

        public int? MonthlySalaryId { get; set; }
        public MonthlySalary? MonthlySalary { get; set; }
    }
}
