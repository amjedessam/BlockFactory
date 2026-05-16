using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Models.Production;

namespace BlockFactory.Core.Interfaces.Repositories
{
    public interface IProductionRepository : IRepository<ProductionRecord>
    {
        Task<IEnumerable<ProductionRecord>> GetByDateAsync(DateTime date);
        Task<IEnumerable<ProductionRecord>> GetByDateRangeAsync(
            DateTime from, DateTime to);
        Task<int> GetTotalProducedAsync(int productId, DateTime from, DateTime to);
        Task<IEnumerable<ProductionRecord>> GetTodayProductionAsync();
    }
}
