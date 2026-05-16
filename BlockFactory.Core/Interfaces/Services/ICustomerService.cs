using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Customers;

using BlockFactory.Core.DTOs.Customers;
using BlockFactory.Core.Common;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface ICustomerService
    {
        // ─── العملاء ────────────────────────────────
        Task<IEnumerable<CustomerListDto>> GetAllCustomersAsync();
        Task<IEnumerable<CustomerListDto>> SearchCustomersAsync(
            string keyword);
        Task<IEnumerable<CustomerListDto>> GetCustomersWithDebtAsync();
        Task<CustomerDetailDto?> GetCustomerDetailAsync(int customerId);
        Task<ServiceResult<int>> CreateCustomerAsync(
            CreateCustomerDto dto);
        Task<ServiceResult> UpdateCustomerAsync(UpdateCustomerDto dto);
        Task<ServiceResult> DeleteCustomerAsync(int customerId);

        // ─── الرهون ─────────────────────────────────
        Task<IEnumerable<PledgeListDto>> GetAllPledgesAsync();
        Task<IEnumerable<PledgeListDto>> GetActivePledgesAsync();
        Task<IEnumerable<PledgeListDto>> GetOverduePledgesAsync();
        Task<ServiceResult> ReturnPledgeAsync(ReturnPledgeDto dto);

        // ─── Lookup ─────────────────────────────────
        Task<IEnumerable<CustomerLookupDto>> GetCustomerLookupsAsync(
            string keyword);
    }
}
