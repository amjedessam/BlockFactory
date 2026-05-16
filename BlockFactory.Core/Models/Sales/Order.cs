using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Customers;

namespace BlockFactory.Core.Models.Sales
{
    public class Order : BaseEntity
    {
        public string OrderNumber { get; set; } = string.Empty;
        // مثال: ORD-2024-0001

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
        // تاريخ الاستحقاق للآجل

        public OrderStatus Status { get; set; }
            = OrderStatus.Pending;

        public PaymentType PaymentType { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
            = PaymentStatus.Unpaid;

        public DeliveryType DeliveryType { get; set; }
        public decimal DeliveryCost { get; set; } = 0;

        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; } = 0;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; } = 0;
        public decimal RemainingAmount { get; set; }

        public string? Notes { get; set; }

        // للدفع الإلكتروني
        public string? ElectronicWalletName { get; set; }
        public string? TransactionReference { get; set; }

        // FK
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        // Navigation
        public ICollection<OrderItem> Items { get; set; }
            = new List<OrderItem>();

        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();

        public Invoice? Invoice { get; set; }
        public Pledge? Pledge { get; set; }
    }
}
