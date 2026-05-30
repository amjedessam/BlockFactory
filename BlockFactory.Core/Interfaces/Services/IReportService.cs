using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.DTOs.Reports;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface IReportService
    {
        Task<InvoiceReportDto?> GetInvoiceDataAsync(int orderId);
        Task<SupplierInvoiceReportDto?> GetSupplierInvoiceDataAsync(int invoiceId);
        Task<DailySalesReportDto> GetDailySalesAsync(DateTime date);
        Task<CustomerDebtReportDto> GetCustomerDebtReportAsync();
        Task<ProductionReportDto> GetProductionReportAsync(
            DateTime from, DateTime to);
        Task<InventoryReportDto> GetInventoryReportAsync();

        // PDF Generation
        Task<byte[]> GenerateInvoicePdfAsync(int orderId);
        Task<byte[]> GenerateSupplierInvoicePdfAsync(int invoiceId);
        Task<byte[]> GenerateDailySalesPdfAsync(DateTime date);
        Task<byte[]> GenerateCustomerDebtPdfAsync();
        Task<byte[]> GenerateProductionPdfAsync(
            DateTime from, DateTime to);
        Task<byte[]> GenerateInventoryPdfAsync();
        Task<byte[]> GenerateSalarySheetPdfAsync(int month, int year);

        // Print
        Task PrintInvoiceAsync(int orderId);
        Task PrintReportAsync(byte[] pdfBytes);
    }
}
