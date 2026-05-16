using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Interfaces.Repositories;
using BlockFactory.Core.Models.Customers;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Data.Repositories
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(AppDbContext context) : base(context) { }

        public async Task<Customer?> GetCustomerWithOrdersAsync(int customerId)
            => await _context.Customers
                .Include(c => c.Orders)
                    .ThenInclude(o => o.Items)
                        .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.Id == customerId);

        public async Task<Customer?> GetCustomerWithPledgesAsync(int customerId)
            => await _context.Customers
                .Include(c => c.Pledges)
                .FirstOrDefaultAsync(c => c.Id == customerId);

        public async Task<IEnumerable<Customer>> GetCustomersWithDebtAsync()
            => await _context.Customers
                .Where(c => c.TotalDebt > 0)
                .OrderByDescending(c => c.TotalDebt)
                .ToListAsync();

        public async Task<IEnumerable<Pledge>> GetActivePledgesAsync()
            => await _context.Pledges
                .Include(p => p.Customer)
                .Where(p => p.Status == PledgeStatus.Active)
                .OrderBy(p => p.DueDate)
                .ToListAsync();

        public async Task<IEnumerable<Pledge>> GetPledgesDueSoonAsync(
            int days = 7)
        {
            var cutoff = DateTime.Today.AddDays(days);
            return await _context.Pledges
                .Include(p => p.Customer)
                .Where(p => p.Status == PledgeStatus.Active &&
                       p.DueDate <= cutoff)
                .OrderBy(p => p.DueDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalDebtAsync()
            => await _context.Customers.SumAsync(c => c.TotalDebt);
    }
}
