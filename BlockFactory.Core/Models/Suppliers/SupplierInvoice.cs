using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Suppliers
{
    public enum SupplierInvoiceStatus
    {
        Unpaid,
        PartiallyPaid,
        FullyPaid
    }

    public class SupplierInvoice : BaseEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; } = 0;
        public decimal RemainingAmount { get; set; }
        public SupplierInvoiceStatus Status { get; set; }
        public string? Notes { get; set; }

        // FK
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        // Navigation
        public ICollection<SupplierInvoiceItem> Items { get; set; }
            = new List<SupplierInvoiceItem>();

        public ICollection<SupplierPayment> Payments { get; set; }
            = new List<SupplierPayment>();
    }
}
