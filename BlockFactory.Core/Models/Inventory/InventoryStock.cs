using System;
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
}
