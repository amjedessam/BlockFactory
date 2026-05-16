using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.HR;
using BlockFactory.Core.DTOs.Orders;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface IHRService
    {
        // ─── العمال ─────────────────────────────────
        Task<IEnumerable<WorkerListDto>> GetAllWorkersAsync();
        Task<WorkerDetailDto?> GetWorkerDetailAsync(int workerId);
        Task<ServiceResult<int>> CreateWorkerAsync(CreateWorkerDto dto);
        Task<ServiceResult> UpdateWorkerAsync(UpdateWorkerDto dto);
        Task<ServiceResult> DeactivateWorkerAsync(int workerId);

        // ─── السلف ──────────────────────────────────
        Task<IEnumerable<AdvanceDto>> GetPendingAdvancesAsync();
        Task<IEnumerable<AdvanceDto>> GetWorkerAdvancesAsync(int workerId);
        Task<ServiceResult> AddAdvanceAsync(CreateAdvanceDto dto);
        Task<ServiceResult> CancelAdvanceAsync(int advanceId);

        // ─── الرواتب ────────────────────────────────
        Task<MonthlySalarySheetDto> GetMonthlySalarySheetAsync(
            int month, int year);
        Task<ServiceResult> GenerateMonthlySalariesAsync(
            GenerateSalariesDto dto);
        Task<ServiceResult> PaySalaryAsync(PaySalaryDto dto);
        Task<ServiceResult> AddDeductionAsync(AddDeductionDto dto);
        Task<ServiceResult> AddBonusAsync(
            int salaryId, decimal amount, string reason);
    }
}
