using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Interfaces.Repositories;
using BlockFactory.Core.Models.Production;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Data.Repositories
{
    public class ProductionRepository
        : Repository<ProductionRecord>, IProductionRepository
    {
        public ProductionRepository(AppDbContext context)
            : base(context) { }

        public async Task<IEnumerable<ProductionRecord>> GetByDateAsync(
            DateTime date)
            => await _context.ProductionRecords
                .Include(p => p.Product)
                .Where(p => p.ProductionDate.Date == date.Date)
                .ToListAsync();

        public async Task<IEnumerable<ProductionRecord>> GetByDateRangeAsync(
            DateTime from, DateTime to)
            => await _context.ProductionRecords
                .Include(p => p.Product)
                .Where(p => p.ProductionDate.Date >= from.Date
                         && p.ProductionDate.Date <= to.Date)
                .OrderByDescending(p => p.ProductionDate)
                .ToListAsync();

        public async Task<int> GetTotalProducedAsync(
            int productId, DateTime from, DateTime to)
            => await _context.ProductionRecords
                .Where(p => p.ProductId == productId
                         && p.ProductionDate.Date >= from.Date
                         && p.ProductionDate.Date <= to.Date)
                .SumAsync(p => p.QuantityNet);

        public async Task<IEnumerable<ProductionRecord>> GetTodayProductionAsync()
            => await _context.ProductionRecords
                .Include(p => p.Product)
                .Where(p => p.ProductionDate.Date == DateTime.Today)
                .ToListAsync();
    }
}