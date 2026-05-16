using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.DTOs.Production;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface IProductionService
    {
        Task<DailyProductionSummaryDto> GetDailySummaryAsync(
            DateTime date);
        Task<IEnumerable<ProductionRecordListDto>> GetByDateRangeAsync(
            DateTime from, DateTime to);
        Task<ProductionStatsDto> GetStatsAsync();
        Task<ServiceResult<int>> CreateProductionRecordAsync(
            CreateProductionDto dto);
        Task<ServiceResult> DeleteProductionRecordAsync(int recordId);
        Task<IEnumerable<CreateMaterialUsageDto>> GetFormulaAsync(
            int productId);
    }
}
