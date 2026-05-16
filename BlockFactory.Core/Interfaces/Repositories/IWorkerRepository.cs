using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Models.HR;

namespace BlockFactory.Core.Interfaces.Repositories
{
    public interface IWorkerRepository : IRepository<Worker>
    {
        Task<Worker?> GetWorkerWithSalariesAsync(int workerId);
        Task<IEnumerable<Advance>> GetPendingAdvancesAsync(int workerId);
        Task<decimal> GetTotalAdvancesAsync(int workerId, int month, int year);
        Task<MonthlySalary?> GetMonthlySalaryAsync(
            int workerId, int month, int year);
        Task<IEnumerable<Worker>> GetActiveWorkersAsync();
    }
}
