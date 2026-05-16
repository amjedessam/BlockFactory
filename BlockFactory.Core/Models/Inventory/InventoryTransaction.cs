using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Inventory
{
    public enum TransactionType
    {
        ProductionIn,   // إضافة من الإنتاج
        SaleOut,        // خصم بيع
        AdjustmentIn,   // تعديل يدوي زيادة
        AdjustmentOut,  // تعديل يدوي نقص
        ReturnIn        // مرتجع من عميل
    }

    public class InventoryTransaction : BaseEntity
    {
        public TransactionType Type { get; set; }
        public int Quantity { get; set; }
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string? Reference { get; set; }
        // رقم الطلب أو رقم دفعة الإنتاج
        public string? Notes { get; set; }

        // FK
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
