using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.DTOs.HR
{
    // ─── العمال ─────────────────────────────────────
    public class WorkerListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? NationalId { get; set; }
        public decimal BasicSalary { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public string HireDateText { get; set; } = string.Empty;
        public decimal PendingAdvances { get; set; }
        public bool HasPendingAdvance { get; set; }
    }

    public class WorkerDetailDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? NationalId { get; set; }
        public decimal BasicSalary { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public string? Notes { get; set; }
        public decimal TotalAdvancesPending { get; set; }
        public List<AdvanceDto> RecentAdvances { get; set; } = new();
        public List<SalaryDto> RecentSalaries { get; set; } = new();
    }

    public class CreateWorkerDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? NationalId { get; set; }
        public decimal BasicSalary { get; set; }
        public DateTime HireDate { get; set; } = DateTime.Today;
        public string? Notes { get; set; }
    }

    public class UpdateWorkerDto : CreateWorkerDto
    {
        public int Id { get; set; }
    }

    // ─── السلف ──────────────────────────────────────
    public class AdvanceDto
    {
        public int Id { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime AdvanceDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateAdvanceDto
    {
        public int WorkerId { get; set; }
        public decimal Amount { get; set; }
        public DateTime AdvanceDate { get; set; } = DateTime.Today;
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }

    // ─── الرواتب ─────────────────────────────────────
    public class SalaryDto
    {
        public int Id { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public decimal Bonus { get; set; }
        public decimal TotalAdvances { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
    }

    public class GenerateSalariesDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
    }

    public class PaySalaryDto
    {
        public int SalaryId { get; set; }
        public decimal PayAmount { get; set; }
        public string? Notes { get; set; }
    }

    public class MonthlySalarySheetDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public List<SalaryDto> Salaries { get; set; } = new();
        public decimal TotalBasic { get; set; }
        public decimal TotalBonus { get; set; }
        public decimal TotalAdvances { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalNet { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
    }

    public class AddDeductionDto
    {
        public int WorkerId { get; set; }
        public Models.HR.DeductionType Type { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
