using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Models.Customers;

namespace BlockFactory.Core.Interfaces.Repositories
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<Customer?> GetCustomerWithOrdersAsync(int customerId);
        Task<Customer?> GetCustomerWithPledgesAsync(int customerId);
        Task<IEnumerable<Customer>> GetCustomersWithDebtAsync();
        Task<IEnumerable<Pledge>> GetActivePledgesAsync();
        Task<IEnumerable<Pledge>> GetPledgesDueSoonAsync(int days = 7);
        Task<decimal> GetTotalDebtAsync();
    }
}
