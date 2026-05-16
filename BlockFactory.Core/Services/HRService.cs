using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.HR;
using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.HR;
using BlockFactory.Core.Session;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Core.Services
{
    public class HRService : IHRService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAuthService _authService;

        public HRService(IUnitOfWork uow, IAuthService authService)
        {
            _uow = uow;
            _authService = authService;
        }

        // ═══════════════════════════════════════════
        // العمال
        // ═══════════════════════════════════════════

        public async Task<IEnumerable<WorkerListDto>> GetAllWorkersAsync()
        {
            var workers = await _uow.Workers.Query()
                .Include(w => w.Advances)
                .OrderBy(w => w.FullName)
                .ToListAsync();

            return workers.Select(w => new WorkerListDto
            {
                Id = w.Id,
                FullName = w.FullName,
                Phone = w.Phone,
                NationalId = w.NationalId,
                BasicSalary = w.BasicSalary,
                Status = GetStatusAr(w.Status),
                StatusColor = GetStatusColor(w.Status),
                HireDate = w.HireDate,
                HireDateText = w.HireDate.ToString("dd/MM/yyyy"),
                PendingAdvances = w.Advances
                    .Where(a => a.Status == AdvanceStatus.Pending)
                    .Sum(a => a.Amount),
                HasPendingAdvance = w.Advances
                    .Any(a => a.Status == AdvanceStatus.Pending)
            });
        }

        public async Task<WorkerDetailDto?> GetWorkerDetailAsync(
            int workerId)
        {
            var worker = await _uow.Workers.Query()
                .Include(w => w.Advances)
                .Include(w => w.Salaries)
                .Include(w => w.Deductions)
                .FirstOrDefaultAsync(w => w.Id == workerId);

            if (worker == null) return null;

            return new WorkerDetailDto
            {
                Id = worker.Id,
                FullName = worker.FullName,
                Phone = worker.Phone,
                Address = worker.Address,
                NationalId = worker.NationalId,
                BasicSalary = worker.BasicSalary,
                Status = GetStatusAr(worker.Status),
                HireDate = worker.HireDate,
                Notes = worker.Notes,
                TotalAdvancesPending = worker.Advances
                    .Where(a => a.Status == AdvanceStatus.Pending)
                    .Sum(a => a.Amount),

                RecentAdvances = worker.Advances
                    .OrderByDescending(a => a.AdvanceDate)
                    .Take(5)
                    .Select(a => new AdvanceDto
                    {
                        Id = a.Id,
                        WorkerName = worker.FullName,
                        Amount = a.Amount,
                        AdvanceDate = a.AdvanceDate,
                        Status = GetAdvanceStatusAr(a.Status),
                        StatusColor = GetAdvanceStatusColor(a.Status),
                        Reason = a.Reason,
                        Notes = a.Notes
                    }).ToList(),

                RecentSalaries = worker.Salaries
                    .OrderByDescending(s => s.Year)
                    .ThenByDescending(s => s.Month)
                    .Take(6)
                    .Select(MapToSalaryDto).ToList()
            };
        }

        public async Task<ServiceResult<int>> CreateWorkerAsync(
            CreateWorkerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                return ServiceResult<int>.Fail("اسم العامل مطلوب");

            if (dto.BasicSalary <= 0)
                return ServiceResult<int>.Fail(
                    "الراتب الأساسي يجب أن يكون أكبر من صفر");

            var worker = new Worker
            {
                FullName = dto.FullName.Trim(),
                Phone = dto.Phone?.Trim(),
                Address = dto.Address?.Trim(),
                NationalId = dto.NationalId?.Trim(),
                BasicSalary = dto.BasicSalary,
                HireDate = dto.HireDate,
                Status = WorkerStatus.Active,
                Notes = dto.Notes,
                CreatedAt = DateTime.Now,
                CreatedByUserId = CurrentSession.Instance.UserId
            };

            await _uow.Workers.AddAsync(worker);
            await _uow.SaveChangesAsync();

            await _authService.LogActivityAsync(
                "CreateWorker", "Worker", worker.Id,
                newValues: worker.FullName);

            return ServiceResult<int>.Ok(worker.Id,
                "تم إضافة العامل بنجاح");
        }

        public async Task<ServiceResult> UpdateWorkerAsync(
            UpdateWorkerDto dto)
        {
            var worker = await _uow.Workers.GetByIdAsync(dto.Id);
            if (worker == null)
                return ServiceResult.Fail("العامل غير موجود");

            if (string.IsNullOrWhiteSpace(dto.FullName))
                return ServiceResult.Fail("اسم العامل مطلوب");

            worker.FullName = dto.FullName.Trim();
            worker.Phone = dto.Phone?.Trim();
            worker.Address = dto.Address?.Trim();
            worker.NationalId = dto.NationalId?.Trim();
            worker.BasicSalary = dto.BasicSalary;
            worker.Notes = dto.Notes;
            worker.UpdatedAt = DateTime.Now;

            _uow.Workers.Update(worker);
            await _uow.SaveChangesAsync();

            return ServiceResult.Ok("تم تحديث بيانات العامل");
        }

        public async Task<ServiceResult> DeactivateWorkerAsync(
            int workerId)
        {
            var worker = await _uow.Workers.GetByIdAsync(workerId);
            if (worker == null)
                return ServiceResult.Fail("العامل غير موجود");

            worker.Status = WorkerStatus.Inactive;
            worker.TerminationDate = DateTime.Today;
            worker.UpdatedAt = DateTime.Now;

            _uow.Workers.Update(worker);
            await _uow.SaveChangesAsync();

            return ServiceResult.Ok("تم إيقاف العامل");
        }

        // ═══════════════════════════════════════════
        // السلف
        // ═══════════════════════════════════════════

        public async Task<IEnumerable<AdvanceDto>> GetPendingAdvancesAsync()
        {
            var advances = await _uow.Advances.Query()
                .Include(a => a.Worker)
                .Where(a => a.Status == AdvanceStatus.Pending)
                .OrderByDescending(a => a.AdvanceDate)
                .ToListAsync();

            return advances.Select(a => new AdvanceDto
            {
                Id = a.Id,
                WorkerName = a.Worker?.FullName ?? "-",
                Amount = a.Amount,
                AdvanceDate = a.AdvanceDate,
                Status = GetAdvanceStatusAr(a.Status),
                StatusColor = GetAdvanceStatusColor(a.Status),
                Reason = a.Reason,
                Notes = a.Notes
            });
        }

        public async Task<IEnumerable<AdvanceDto>> GetWorkerAdvancesAsync(
            int workerId)
        {
            var advances = await _uow.Advances.Query()
                .Include(a => a.Worker)
                .Where(a => a.WorkerId == workerId)
                .OrderByDescending(a => a.AdvanceDate)
                .ToListAsync();

            return advances.Select(a => new AdvanceDto
            {
                Id = a.Id,
                WorkerName = a.Worker?.FullName ?? "-",
                Amount = a.Amount,
                AdvanceDate = a.AdvanceDate,
                Status = GetAdvanceStatusAr(a.Status),
                StatusColor = GetAdvanceStatusColor(a.Status),
                Reason = a.Reason,
                Notes = a.Notes
            });
        }

        public async Task<ServiceResult> AddAdvanceAsync(
            CreateAdvanceDto dto)
        {
            if (dto.Amount <= 0)
                return ServiceResult.Fail(
                    "مبلغ السلفة يجب أن يكون أكبر من صفر");

            var worker = await _uow.Workers.GetByIdAsync(dto.WorkerId);
            if (worker == null)
                return ServiceResult.Fail("العامل غير موجود");

            if (worker.Status != WorkerStatus.Active)
                return ServiceResult.Fail("العامل غير نشط");

            // التحقق من أن السلفة لا تتجاوز الراتب
            var pendingAdvances = await _uow.Advances.Query()
                .Where(a =>
                    a.WorkerId == dto.WorkerId &&
                    a.Status == AdvanceStatus.Pending)
                .SumAsync(a => a.Amount);

            if (pendingAdvances + dto.Amount > worker.BasicSalary)
                return ServiceResult.Fail(
                    $"إجمالي السلف ({pendingAdvances + dto.Amount:N0} ر.ي) " +
                    $"يتجاوز الراتب الأساسي ({worker.BasicSalary:N0} ر.ي)");

            var advance = new Advance
            {
                WorkerId = dto.WorkerId,
                Amount = dto.Amount,
                AdvanceDate = dto.AdvanceDate,
                Status = AdvanceStatus.Pending,
                Reason = dto.Reason,
                Notes = dto.Notes,
                CreatedAt = DateTime.Now,
                CreatedByUserId = CurrentSession.Instance.UserId
            };

            await _uow.Advances.AddAsync(advance);
            await _uow.SaveChangesAsync();

            await _authService.LogActivityAsync(
                "AddAdvance", "Worker", dto.WorkerId,
                newValues: $"سلفة: {dto.Amount:N0} ر.ي");

            return ServiceResult.Ok(
                $"تم تسجيل سلفة {dto.Amount:N0} ر.ي " +
                $"للعامل {worker.FullName}");
        }

        public async Task<ServiceResult> CancelAdvanceAsync(int advanceId)
        {
            var advance = await _uow.Advances.GetByIdAsync(advanceId);
            if (advance == null)
                return ServiceResult.Fail("السلفة غير موجودة");

            if (advance.Status == AdvanceStatus.Deducted)
                return ServiceResult.Fail("لا يمكن إلغاء سلفة تم خصمها");

            advance.Status = AdvanceStatus.Cancelled;
            advance.UpdatedAt = DateTime.Now;
            _uow.Advances.Update(advance);
            await _uow.SaveChangesAsync();

            return ServiceResult.Ok("تم إلغاء السلفة");
        }

        // ═══════════════════════════════════════════
        // الرواتب
        // ═══════════════════════════════════════════

        public async Task<MonthlySalarySheetDto>
            GetMonthlySalarySheetAsync(int month, int year)
        {
            var salaries = await _uow.Salaries.Query()
                .Include(s => s.Worker)
                .Where(s => s.Month == month && s.Year == year)
                .OrderBy(s => s.Worker.FullName)
                .ToListAsync();

            var salaryDtos = salaries.Select(MapToSalaryDto).ToList();

            return new MonthlySalarySheetDto
            {
                Month = month,
                Year = year,
                MonthName = GetMonthName(month, year),
                Salaries = salaryDtos,
                TotalBasic = salaryDtos.Sum(s => s.BasicSalary),
                TotalBonus = salaryDtos.Sum(s => s.Bonus),
                TotalAdvances = salaryDtos.Sum(s => s.TotalAdvances),
                TotalDeductions = salaryDtos.Sum(s => s.TotalDeductions),
                TotalNet = salaryDtos.Sum(s => s.NetSalary),
                TotalPaid = salaryDtos.Sum(s => s.PaidAmount),
                TotalRemaining = salaryDtos.Sum(s => s.RemainingAmount)
            };
        }

        public async Task<ServiceResult> GenerateMonthlySalariesAsync(
            GenerateSalariesDto dto)
        {
            // التحقق من عدم وجود كشف لهذا الشهر
            bool exists = await _uow.Salaries.AnyAsync(
                s => s.Month == dto.Month && s.Year == dto.Year);

            if (exists)
                return ServiceResult.Fail(
                    $"كشف رواتب {GetMonthName(dto.Month, dto.Year)} " +
                    $"موجود مسبقاً");

            var workers = await _uow.Workers.Query()
                .Include(w => w.Advances)
                .Include(w => w.Deductions)
                .Where(w => w.Status == WorkerStatus.Active)
                .ToListAsync();

            if (!workers.Any())
                return ServiceResult.Fail("لا يوجد عمال نشطون");

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var worker in workers)
                {
                    // السلف المستحقة هذا الشهر
                    var pendingAdvances = worker.Advances
                        .Where(a => a.Status == AdvanceStatus.Pending)
                        .Sum(a => a.Amount);

                    // الخصومات الأخرى
                    var deductions = worker.Deductions
                        .Where(d =>
                            d.Month == dto.Month &&
                            d.Year == dto.Year)
                        .Sum(d => d.Amount);

                    var netSalary = worker.BasicSalary
                        - pendingAdvances
                        - deductions;

                    if (netSalary < 0) netSalary = 0;

                    var salary = new MonthlySalary
                    {
                        WorkerId = worker.Id,
                        Month = dto.Month,
                        Year = dto.Year,
                        BasicSalary = worker.BasicSalary,
                        Bonus = 0,
                        TotalAdvances = pendingAdvances,
                        TotalDeductions = deductions,
                        NetSalary = netSalary,
                        PaidAmount = 0,
                        RemainingAmount = netSalary,
                        Status = SalaryStatus.Pending,
                        CreatedAt = DateTime.Now,
                        CreatedByUserId = CurrentSession.Instance.UserId
                    };

                    await _uow.Salaries.AddAsync(salary);

                    // تحديث حالة السلف إلى مخصومة
                    var pendingAdvancesList = worker.Advances
                        .Where(a => a.Status == AdvanceStatus.Pending)
                        .ToList();

                    foreach (var advance in pendingAdvancesList)
                    {
                        advance.Status = AdvanceStatus.Deducted;
                        advance.DeductionDate = DateTime.Now;
                        advance.UpdatedAt = DateTime.Now;
                        _uow.Advances.Update(advance);
                    }
                }

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                await _authService.LogActivityAsync(
                    "GenerateSalaries",
                    "HR",
                    newValues:
                        $"كشف رواتب {GetMonthName(dto.Month, dto.Year)}");

                return ServiceResult.Ok(
                    $"تم إنشاء كشف رواتب " +
                    $"{GetMonthName(dto.Month, dto.Year)} " +
                    $"لـ {workers.Count} عامل");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult.Fail($"خطأ: {ex.Message}");
            }
        }

        public async Task<ServiceResult> PaySalaryAsync(PaySalaryDto dto)
        {
            var salary = await _uow.Salaries.Query()
                .Include(s => s.Worker)
                .FirstOrDefaultAsync(s => s.Id == dto.SalaryId);

            if (salary == null)
                return ServiceResult.Fail("السجل غير موجود");

            if (salary.Status == SalaryStatus.Paid)
                return ServiceResult.Fail("الراتب مصروف بالفعل");

            if (dto.PayAmount <= 0)
                return ServiceResult.Fail("المبلغ يجب أن يكون أكبر من صفر");

            if (dto.PayAmount > salary.RemainingAmount)
                return ServiceResult.Fail(
                    $"المبلغ أكبر من المتبقي " +
                    $"({salary.RemainingAmount:N0} ر.ي)");

            salary.PaidAmount += dto.PayAmount;
            salary.RemainingAmount -= dto.PayAmount;
            salary.Status = salary.RemainingAmount <= 0
                ? SalaryStatus.Paid
                : SalaryStatus.PartiallyPaid;

            if (salary.Status == SalaryStatus.Paid)
                salary.PaidAt = DateTime.Now;

            salary.UpdatedAt = DateTime.Now;
            _uow.Salaries.Update(salary);

            // تسجيل مصروف الراتب
            var expense = new Models.Finance.Expense
            {
                Category = Models.Finance.ExpenseCategory.Other,
                Amount = dto.PayAmount,
                ExpenseDate = DateTime.Now,
                Description =
                    $"راتب {salary.Worker?.FullName} — " +
                    $"{GetMonthName(salary.Month, salary.Year)}",
                Notes = dto.Notes,
                CreatedAt = DateTime.Now,
                CreatedByUserId = CurrentSession.Instance.UserId
            };
            await _uow.Expenses.AddAsync(expense);

            await _uow.SaveChangesAsync();

            return ServiceResult.Ok(
                $"تم صرف {dto.PayAmount:N0} ر.ي " +
                $"لـ {salary.Worker?.FullName}");
        }

        public async Task<ServiceResult> AddDeductionAsync(
            AddDeductionDto dto)
        {
            if (dto.Amount <= 0)
                return ServiceResult.Fail("مبلغ الخصم يجب أن يكون أكبر من صفر");

            var worker = await _uow.Workers.GetByIdAsync(dto.WorkerId);
            if (worker == null)
                return ServiceResult.Fail("العامل غير موجود");

            var deduction = new Deduction
            {
                WorkerId = dto.WorkerId,
                Type = dto.Type,
                Amount = dto.Amount,
                Reason = dto.Reason,
                Month = dto.Month,
                Year = dto.Year,
                DeductionDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            await _uow.Deductions.AddAsync(deduction);
            await _uow.SaveChangesAsync();

            return ServiceResult.Ok(
                $"تم إضافة خصم {dto.Amount:N0} ر.ي " +
                $"للعامل {worker.FullName}");
        }

        public async Task<ServiceResult> AddBonusAsync(
            int salaryId, decimal amount, string reason)
        {
            if (amount <= 0)
                return ServiceResult.Fail(
                    "المكافأة يجب أن تكون أكبر من صفر");

            var salary = await _uow.Salaries.GetByIdAsync(salaryId);
            if (salary == null)
                return ServiceResult.Fail("السجل غير موجود");

            if (salary.Status == SalaryStatus.Paid)
                return ServiceResult.Fail("لا يمكن تعديل راتب مصروف");

            salary.Bonus += amount;
            salary.NetSalary += amount;
            salary.RemainingAmount += amount;
            salary.UpdatedAt = DateTime.Now;

            _uow.Salaries.Update(salary);
            await _uow.SaveChangesAsync();

            return ServiceResult.Ok($"تم إضافة مكافأة {amount:N0} ر.ي");
        }

        // ─── Helpers ────────────────────────────────
        private static SalaryDto MapToSalaryDto(MonthlySalary s) => new()
        {
            Id = s.Id,
            WorkerName = s.Worker?.FullName ?? "-",
            Month = s.Month,
            Year = s.Year,
            MonthName = GetMonthName(s.Month, s.Year),
            BasicSalary = s.BasicSalary,
            Bonus = s.Bonus,
            TotalAdvances = s.TotalAdvances,
            TotalDeductions = s.TotalDeductions,
            NetSalary = s.NetSalary,
            PaidAmount = s.PaidAmount,
            RemainingAmount = s.RemainingAmount,
            Status = GetSalaryStatusAr(s.Status),
            StatusColor = GetSalaryStatusColor(s.Status),
            PaidAt = s.PaidAt
        };

        private static string GetStatusAr(WorkerStatus s) => s switch
        {
            WorkerStatus.Active => "نشط",
            WorkerStatus.Inactive => "موقوف",
            WorkerStatus.Terminated => "منتهي",
            _ => "-"
        };

        private static string GetStatusColor(WorkerStatus s) => s switch
        {
            WorkerStatus.Active => "#27AE60",
            WorkerStatus.Inactive => "#F39C12",
            WorkerStatus.Terminated => "#E74C3C",
            _ => "#95A5A6"
        };

        private static string GetAdvanceStatusAr(AdvanceStatus s) => s switch
        {
            AdvanceStatus.Pending => "معلقة",
            AdvanceStatus.Deducted => "مخصومة",
            AdvanceStatus.Cancelled => "ملغية",
            _ => "-"
        };

        private static string GetAdvanceStatusColor(AdvanceStatus s) => s switch
        {
            AdvanceStatus.Pending => "#F39C12",
            AdvanceStatus.Deducted => "#27AE60",
            AdvanceStatus.Cancelled => "#95A5A6",
            _ => "#95A5A6"
        };

        private static string GetSalaryStatusAr(SalaryStatus s) => s switch
        {
            SalaryStatus.Pending => "لم يُصرف",
            SalaryStatus.Paid => "مصروف",
            SalaryStatus.PartiallyPaid => "جزئي",
            _ => "-"
        };

        private static string GetSalaryStatusColor(SalaryStatus s) => s switch
        {
            SalaryStatus.Pending => "#E74C3C",
            SalaryStatus.Paid => "#27AE60",
            SalaryStatus.PartiallyPaid => "#F39C12",
            _ => "#95A5A6"
        };

        private static string GetMonthName(int month, int year)
        {
            var date = new DateTime(year, month, 1);
            return date.ToString("MMMM yyyy",
                new System.Globalization.CultureInfo("ar-SA"));
        }
    }
}
