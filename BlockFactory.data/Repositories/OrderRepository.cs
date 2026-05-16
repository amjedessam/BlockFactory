using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Interfaces.Repositories;
using BlockFactory.Core.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Data.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext context) : base(context) { }

        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
            => await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Payments)
                .Include(o => o.Invoice)
                .Include(o => o.Pledge)
                .FirstOrDefaultAsync(o => o.Id == orderId);

        public async Task<IEnumerable<Order>> GetOrdersByCustomerAsync(
            int customerId)
            => await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

        public async Task<IEnumerable<Order>> GetOrdersByDateAsync(
            DateTime from, DateTime to)
            => await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .Where(o => o.OrderDate >= from && o.OrderDate <= to)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

        public async Task<IEnumerable<Order>> GetUnpaidOrdersAsync()
            => await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.PaymentStatus != PaymentStatus.FullyPaid)
                .OrderBy(o => o.DueDate)
                .ToListAsync();

        public async Task<IEnumerable<Order>> GetOrdersDueTodayAsync()
            => await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.DueDate.HasValue &&
                       o.DueDate.Value.Date == DateTime.Today &&
                       o.PaymentStatus != PaymentStatus.FullyPaid)
                .ToListAsync();

        public async Task<string> GenerateOrderNumberAsync()
        {
            var year = DateTime.Now.Year;
            var count = await _context.Orders
                .CountAsync(o => o.CreatedAt.Year == year);

            return $"ORD-{year}-{(count + 1):D4}";
        }

        public async Task<decimal> GetTotalRevenueAsync(
            DateTime from, DateTime to)
            => await _context.Orders
                .Where(o => o.OrderDate >= from &&
                       o.OrderDate <= to &&
                       o.PaymentStatus == PaymentStatus.FullyPaid)
                .SumAsync(o => o.TotalAmount);
    }
}
