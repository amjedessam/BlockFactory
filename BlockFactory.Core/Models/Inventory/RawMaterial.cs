using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Inventory
{
    public enum MaterialUnit
    {
        Bag,    // كيس (إسمنت)
        Ton,    // طن (رمل، حصى)
        Cubic,  // متر مكعب
        Liter,  // لتر
        Other
    }

    public class RawMaterial : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        // إسمنت، رمل، حصى، ماء

        public MaterialUnit Unit { get; set; }
        public decimal QuantityAvailable { get; set; } = 0;
        public decimal MinimumThreshold { get; set; } = 0;
        public decimal CurrentUnitCost { get; set; } = 0;
        // سعر الوحدة الحالي

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<RawMaterialTransaction> Transactions { get; set; }
            = new List<RawMaterialTransaction>();

        public ICollection<ProductionMaterialUsage> Usages { get; set; }
            = new List<ProductionMaterialUsage>();

        public ICollection<ProductionFormula> Formulas { get; set; }
            = new List<ProductionFormula>();
    }
}
