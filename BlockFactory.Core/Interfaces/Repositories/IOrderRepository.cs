using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Models.Sales;

namespace BlockFactory.Core.Interfaces.Repositories
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetOrderWithDetailsAsync(int orderId);
        Task<IEnumerable<Order>> GetOrdersByCustomerAsync(int customerId);
        Task<IEnumerable<Order>> GetOrdersByDateAsync(
            DateTime from, DateTime to);
        Task<IEnumerable<Order>> GetUnpaidOrdersAsync();
        Task<IEnumerable<Order>> GetOrdersDueTodayAsync();
        Task<string> GenerateOrderNumberAsync();
        Task<decimal> GetTotalRevenueAsync(DateTime from, DateTime to);
    }
}
