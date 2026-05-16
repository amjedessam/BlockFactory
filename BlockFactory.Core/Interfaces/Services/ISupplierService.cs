using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.DTOs.Suppliers;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface ISupplierService
    {
        Task<SuppliersSummaryDto> GetSummaryAsync();
        Task<IEnumerable<SupplierListDto>> GetAllSuppliersAsync();
        Task<IEnumerable<SupplierListDto>> SearchSuppliersAsync(
            string keyword);
        Task<SupplierDetailDto?> GetSupplierDetailAsync(int supplierId);
        Task<ServiceResult<int>> CreateSupplierAsync(
            CreateSupplierDto dto);
        Task<ServiceResult> UpdateSupplierAsync(
            int id, CreateSupplierDto dto);
        Task<IEnumerable<SupplierInvoiceDto>> GetSupplierInvoicesAsync(
            int supplierId);
        Task<ServiceResult<int>> CreateInvoiceAsync(
            CreateSupplierInvoiceDto dto);
        Task<IReadOnlyList<RawMaterialLookupDto>> GetActiveRawMaterialsAsync();
        Task<ServiceResult> PaySupplierAsync(PaySupplierDto dto);
        Task<IEnumerable<SupplierListDto>> GetSuppliersWithDebtAsync();
    }
}

