using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.HR
{
    public enum SalaryStatus
    {
        Pending,    // لم يُصرف
        Paid,       // مصروف
        PartiallyPaid
    }

    public class MonthlySalary : BaseEntity
    {
        public int Month { get; set; }       // 1-12
        public int Year { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Bonus { get; set; } = 0;
        // مكافأة

        public decimal TotalAdvances { get; set; } = 0;
        // إجمالي السلف المخصومة هذا الشهر

        public decimal TotalDeductions { get; set; } = 0;
        // خصومات أخرى

        public decimal NetSalary { get; set; }
        // = BasicSalary + Bonus - TotalAdvances - TotalDeductions

        public decimal PaidAmount { get; set; } = 0;
        public decimal RemainingAmount { get; set; } = 0;
        public SalaryStatus Status { get; set; } = SalaryStatus.Pending;
        public DateTime? PaidAt { get; set; }
        public string? Notes { get; set; }

        // FK
        public int WorkerId { get; set; }
        public Worker Worker { get; set; } = null!;
    }
}
