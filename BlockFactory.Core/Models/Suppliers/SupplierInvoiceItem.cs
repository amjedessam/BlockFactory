using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Suppliers
{
    public class SupplierInvoiceItem : BaseEntity
    {
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Description { get; set; }

        // FK
        public int SupplierInvoiceId { get; set; }
        public SupplierInvoice SupplierInvoice { get; set; } = null!;

        public int? RawMaterialId { get; set; }
        public RawMaterial? RawMaterial { get; set; }
    }
}
