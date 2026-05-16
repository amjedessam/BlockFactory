using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Inventory;

namespace BlockFactory.Core.Models.Production
{
    public class ProductionMaterialUsage : BaseEntity
    {
        public decimal QuantityUsed { get; set; }
        public string Unit { get; set; } = string.Empty;
        // كيس، طن، م³

        // FK
        public int ProductionRecordId { get; set; }
        public ProductionRecord ProductionRecord { get; set; } = null!;

        public int RawMaterialId { get; set; }
        public RawMaterial RawMaterial { get; set; } = null!;
    }
}
