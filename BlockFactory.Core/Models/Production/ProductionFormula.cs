using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Inventory;
using BlockFactory.Core.Models.Products;

namespace BlockFactory.Core.Models.Production
{
    public class ProductionFormula : BaseEntity
    {
        public int QuantityPerBatch { get; set; }
        // عدد البلوك لكل دفعة

        public decimal MaterialQuantity { get; set; }
        // كمية المادة لكل دفعة

        public string Unit { get; set; } = string.Empty;

        // FK
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int RawMaterialId { get; set; }
        public RawMaterial RawMaterial { get; set; } = null!;
    }
}
