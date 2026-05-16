using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Base;

namespace BlockFactory.Core.Models.Products
{
    // صم / مخرق ثقيل / مخرق خفيف / هردي
    public class ProductType : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}
