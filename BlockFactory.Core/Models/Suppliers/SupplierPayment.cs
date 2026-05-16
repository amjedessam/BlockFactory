using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Suppliers
{
    public class SupplierPayment : BaseEntity
    {
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string? Method { get; set; }
        // نقد، تحويل...
        public string? Reference { get; set; }
        public string? Notes { get; set; }

        // FK
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public int? SupplierInvoiceId { get; set; }
        public SupplierInvoice? SupplierInvoice { get; set; }
    }
}
