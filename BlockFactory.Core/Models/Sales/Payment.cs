using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Base;
namespace BlockFactory.Core.Models.Sales
{
    public enum PaymentMethod
    {
        Cash,
        Electronic,
        Mixed
    }

    public class Payment : BaseEntity
    {
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public PaymentMethod Method { get; set; }
        public string? WalletName { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }

        // FK
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
    }
}
