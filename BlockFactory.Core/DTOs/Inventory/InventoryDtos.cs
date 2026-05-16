using System;
using System.Collections.Generic;

namespace BlockFactory.Core.DTOs.Inventory
{
    public class InventorySummaryDto
    {
        public int ProductSkuCount { get; set; }
        public int TotalUnitsAvailable { get; set; }
        public int LowStockProductCount { get; set; }
        public int LowRawMaterialCount { get; set; }
    }

    public class InventoryProductRowDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantityAvailable { get; set; }
        public int MinimumThreshold { get; set; }
    }

    public class InventoryMaterialRowDto
    {
        public int RawMaterialId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal QuantityAvailable { get; set; }
        public decimal MinimumThreshold { get; set; }
    }
}
