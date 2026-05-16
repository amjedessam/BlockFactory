using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.DTOs.Production
{
    public class ProductionRecordListDto
    {
        public int Id { get; set; }
        public DateTime ProductionDate { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty;
        public int QuantityProduced { get; set; }
        public int QuantityDefective { get; set; }
        public int QuantityNet { get; set; }
        public string? Notes { get; set; }
        public List<MaterialUsageDto> MaterialUsages { get; set; } = new();
    }

    public class MaterialUsageDto
    {
        public string MaterialName { get; set; } = string.Empty;
        public decimal QuantityUsed { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class DailyProductionSummaryDto
    {
        public DateTime Date { get; set; }
        public int TotalProduced { get; set; }
        public int TotalDefective { get; set; }
        public int TotalNet { get; set; }
        public List<ProductionRecordListDto> Records { get; set; } = new();
        public List<MaterialConsumptionDto> MaterialsConsumed { get; set; }
            = new();
    }

    public class MaterialConsumptionDto
    {
        public string MaterialName { get; set; } = string.Empty;
        public decimal TotalConsumed { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal RemainingStock { get; set; }
        public bool IsLow { get; set; }
    }

    public class CreateProductionDto
    {
        public DateTime ProductionDate { get; set; } = DateTime.Today;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public Models.Production.ProductionShift Shift { get; set; }
        public int QuantityProduced { get; set; }
        public int QuantityDefective { get; set; } = 0;
        public string? Notes { get; set; }
        public List<CreateMaterialUsageDto> MaterialUsages { get; set; }
            = new();
    }

    public class CreateMaterialUsageDto
    {
        public int RawMaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public decimal QuantityUsed { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class ProductionStatsDto
    {
        public int TodayTotal { get; set; }
        public int WeekTotal { get; set; }
        public int MonthTotal { get; set; }
        public int TodayDefective { get; set; }
        public double DefectiveRate { get; set; }
        public List<ProductionByTypeDto> ByProductType { get; set; } = new();
    }

    public class ProductionByTypeDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int TodayQty { get; set; }
        public int MonthQty { get; set; }
        public int StockQty { get; set; }
    }
}
