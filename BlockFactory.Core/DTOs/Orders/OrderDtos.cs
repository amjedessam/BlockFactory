using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Common;
using BlockFactory.Core.Models.Sales;

namespace BlockFactory.Core.DTOs.Orders
{
    // ─── قائمة الطلبات ──────────────────────────
    public class OrderListDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentStatusColor { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public string DeliveryType { get; set; } = string.Empty;
        public bool HasPledge { get; set; }
        public bool IsOverdue { get; set; }
    }

    // ─── تفاصيل طلب ─────────────────────────────
    public class OrderDetailDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? DueDate { get; set; }
        public PaymentType PaymentType { get; set; }
        public string PaymentTypeName { get; set; } = string.Empty;
        public PaymentStatus PaymentStatus { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public decimal DeliveryCost { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string? ElectronicWalletName { get; set; }
        public string? TransactionReference { get; set; }
        public string? Notes { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public List<PaymentDto> Payments { get; set; } = new();
        public PledgeDto? Pledge { get; set; }
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class PaymentDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Method { get; set; } = string.Empty;
        public string? WalletName { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }

    public class PledgeDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PledgeType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string? Notes { get; set; }
    }

    // ─── إنشاء طلب جديد ─────────────────────────
    public class CreateOrderDto
    {
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
        public PaymentType PaymentType { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public decimal DeliveryCost { get; set; } = 0;
        public decimal Discount { get; set; } = 0;
        public string? ElectronicWalletName { get; set; }
        public string? TransactionReference { get; set; }
        public string? Notes { get; set; }
        public decimal InitialPayment { get; set; } = 0;
        public List<CreateOrderItemDto> Items { get; set; } = new();

        // بيانات الرهن (إن وجد)
        public CreatePledgeDto? Pledge { get; set; }
    }

    public class CreateOrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
    }

    public class CreatePledgeDto
    {
        public string Description { get; set; } = string.Empty;
        public Models.Customers.PledgeType PledgeType { get; set; }
        public string? PledgeTypeOther { get; set; }
        public DateTime DueDate { get; set; }
        public string? Notes { get; set; }
    }

    // ─── إضافة دفعة ─────────────────────────────
    public class AddPaymentDto
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public Models.Sales.PaymentMethod Method { get; set; }
        public string? WalletName { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }

    // ─── نتيجة العملية ────

}
