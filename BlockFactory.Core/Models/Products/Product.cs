using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Inventory;
using BlockFactory.Core.Models.Production;
using BlockFactory.Core.Models.Sales;

namespace BlockFactory.Core.Models.Products
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        // "بلك صم أبو 10"

        public int Size { get; set; }
        // 10, 15, 20, 25

        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
        public decimal DefaultPrice { get; set; }

        public bool IsActive { get; set; } = true;


        // FK
        public int ProductTypeId { get; set; }
        public ProductType ProductType { get; set; } = null!;

        // Navigation
        public ICollection<OrderItem> OrderItems { get; set; }
            = new List<OrderItem>();

        public ICollection<ProductionRecord> ProductionRecords { get; set; }
            = new List<ProductionRecord>();

        public InventoryStock? Stock { get; set; }
    }
}
