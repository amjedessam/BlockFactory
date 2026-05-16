using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.DTOs.Dashboard;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync();
        Task<IEnumerable<RecentOrderDto>> GetRecentOrdersAsync(int count = 8);
        Task<IEnumerable<AlertDto>> GetAlertsAsync();
    }
}
