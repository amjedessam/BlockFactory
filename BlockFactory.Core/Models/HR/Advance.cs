using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.HR
{
    public enum AdvanceStatus
    {
        Pending,      // لم يُخصم بعد
        Deducted,     // تم الخصم
        Cancelled     // ملغي
    }

    public class Advance : BaseEntity
    {
        public decimal Amount { get; set; }
        public DateTime AdvanceDate { get; set; } = DateTime.Now;
        public DateTime? DeductionDate { get; set; }
        // الشهر الذي سيُخصم فيه

        public AdvanceStatus Status { get; set; } = AdvanceStatus.Pending;
        public string? Reason { get; set; }
        public string? Notes { get; set; }

        // FK
        public int WorkerId { get; set; }
        public Worker Worker { get; set; } = null!;

        public int? MonthlySalaryId { get; set; }
        public MonthlySalary? MonthlySalary { get; set; }
        // مرتبط بكشف الراتب الذي خُصم فيه
    }
}
