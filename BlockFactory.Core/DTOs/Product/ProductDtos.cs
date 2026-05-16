using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.DTOs.Products
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public int ProductTypeId { get; set; }
        public int Size { get; set; }
        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
        public decimal DefaultPrice { get; set; }
        public bool IsActive { get; set; }
        public int StockQuantity { get; set; }
    }

    public class UpdateProductPriceDto
    {
        public int ProductId { get; set; }
        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
        public decimal DefaultPrice { get; set; }
    }

    public class UpdateStockThresholdDto
    {
        public int ProductId { get; set; }
        public int MinimumThreshold { get; set; }
    }
}
