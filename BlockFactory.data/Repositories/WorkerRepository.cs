using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Interfaces.Repositories;
using BlockFactory.Core.Models.HR;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Data.Repositories
{
    public class WorkerRepository
        : Repository<Worker>, IWorkerRepository
    {
        public WorkerRepository(AppDbContext context)
            : base(context) { }

        public async Task<Worker?> GetWorkerWithSalariesAsync(int workerId)
            => await _context.Workers
                .Include(w => w.Salaries)
                .Include(w => w.Advances)
                .Include(w => w.Deductions)
                .FirstOrDefaultAsync(w => w.Id == workerId);

        public async Task<IEnumerable<Advance>> GetPendingAdvancesAsync(
            int workerId)
            => await _context.Advances
                .Where(a => a.WorkerId == workerId
                         && a.Status == AdvanceStatus.Pending)
                .ToListAsync();

        public async Task<decimal> GetTotalAdvancesAsync(
            int workerId, int month, int year)
            => await _context.Advances
                .Where(a => a.WorkerId == workerId
                         && a.AdvanceDate.Month == month
                         && a.AdvanceDate.Year == year)
                .SumAsync(a => a.Amount);

        public async Task<MonthlySalary?> GetMonthlySalaryAsync(
            int workerId, int month, int year)
            => await _context.MonthlySalaries
                .FirstOrDefaultAsync(s => s.WorkerId == workerId
                                       && s.Month == month
                                       && s.Year == year);

        public async Task<IEnumerable<Worker>> GetActiveWorkersAsync()
            => await _context.Workers
                .Where(w => w.Status == WorkerStatus.Active
                         && !w.IsDeleted)
                .ToListAsync();
    }
}