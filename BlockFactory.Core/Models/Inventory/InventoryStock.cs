/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Products;

namespace BlockFactory.Core.Models.Inventory
{
    public class InventoryStock : BaseEntity
    {
        public int QuantityAvailable { get; set; } = 0;
        public int MinimumThreshold { get; set; } = 0;
        // حد التنبيه الأدنى

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // FK — علاقة واحد لواحد مع المنتج
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}*/

// BlockFactory.Core/Models/Inventory/InventoryStock.cs

using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Products;

namespace BlockFactory.Core.Models.Inventory
{
    public class InventoryStock : BaseEntity
    {
        // ─── المخزون الفعلي ─────────────────────────
        /// <summary>الكمية المتاحة فعلياً في المستودع</summary>
        public int QuantityAvailable { get; set; } = 0;

        // ─── الالتزامات ─────────────────────────────
        /// <summary>
        /// الكمية المحجوزة من فواتير الحجز المحدد (QuantityReservation)
        /// هذه ليست كمية مستهلكة — هي "التزام مستقبلي"
        /// تزداد عند إنشاء حجز محدد
        /// تنقص عند كل سحب مكتمل
        /// لا تتأثر بفواتير الحجز المفتوح (OpenBalance)
        /// </summary>
        public int ReservedQuantity { get; set; } = 0;

        // ─── حساب الكمية الحرة ──────────────────────
        /// <summary>
        /// الكمية الحرة = Available - Reserved
        /// هذه هي الكمية القابلة للبيع أو للحجز الجديد
        /// </summary>
        public int FreeQuantity => QuantityAvailable - ReservedQuantity;

        // ─── حد التنبيه ─────────────────────────────
        public int MinimumThreshold { get; set; } = 0;

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // ─── FK ─────────────────────────────────────
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}

