using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Products;

namespace BlockFactory.Core.Models.Production
{
    public enum ProductionShift
    {
        Morning,   // صباحي
        Evening    // مسائي
    }

    public class ProductionRecord : BaseEntity
    {
        public DateTime ProductionDate { get; set; } = DateTime.Today;
        public ProductionShift Shift { get; set; }
        public int QuantityProduced { get; set; }
        public int QuantityDefective { get; set; } = 0;
        // الكمية التالفة

        public int QuantityNet { get; set; }
        // = QuantityProduced - QuantityDefective

        public string? Notes { get; set; }

        // FK
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // المواد المستهلكة في هذا الإنتاج
        public ICollection<ProductionMaterialUsage> MaterialUsages { get; set; }
            = new List<ProductionMaterialUsage>();
    }
}
