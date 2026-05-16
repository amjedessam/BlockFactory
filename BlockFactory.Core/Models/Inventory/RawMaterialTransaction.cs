using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Suppliers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Inventory
{
    public enum RawMaterialTransactionType
    {
        PurchaseIn,     // شراء من مورد
        ProductionOut,  // استهلاك في الإنتاج
        AdjustmentIn,   // تعديل يدوي زيادة
        AdjustmentOut   // تعديل يدوي نقص
    }

    public class RawMaterialTransaction : BaseEntity
    {
        public RawMaterialTransactionType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityBefore { get; set; }
        public decimal QuantityAfter { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string? Reference { get; set; }
        public string? Notes { get; set; }

        // FK
        public int RawMaterialId { get; set; }
        public RawMaterial RawMaterial { get; set; } = null!;

        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
    }
}
