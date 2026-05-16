using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.HR
{
    public enum WorkerStatus
    {
        Active,     // نشط
        Inactive,   // غير نشط
        Terminated  // منتهي الخدمة
    }

    public class Worker : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? NationalId { get; set; }
        public DateTime HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public decimal BasicSalary { get; set; }
        public WorkerStatus Status { get; set; } = WorkerStatus.Active;
        public string? Notes { get; set; }

        // Navigation
        public ICollection<MonthlySalary> Salaries { get; set; }
            = new List<MonthlySalary>();

        public ICollection<Advance> Advances { get; set; }
            = new List<Advance>();

        public ICollection<Deduction> Deductions { get; set; }
            = new List<Deduction>();
    }
}