using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.Orders;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderListDto>> GetAllOrdersAsync();
        Task<IEnumerable<OrderListDto>> GetOrdersByDateAsync(
            DateTime from, DateTime to);
        Task<IEnumerable<OrderListDto>> SearchOrdersAsync(string keyword);
        Task<OrderDetailDto?> GetOrderDetailAsync(int orderId);
        Task<ServiceResult<int>> CreateOrderAsync(CreateOrderDto dto);
        Task<ServiceResult> AddPaymentAsync(AddPaymentDto dto);
        Task<ServiceResult> CancelOrderAsync(int orderId, string reason);
        Task<ServiceResult> UpdateOrderStatusAsync(
            int orderId, string status);
        Task<byte[]> GenerateInvoicePdfAsync(int orderId);
    }
}
