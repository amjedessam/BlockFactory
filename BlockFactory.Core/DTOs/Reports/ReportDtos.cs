using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.DTOs.Reports
{
    public class ReportRequestDto
    {
        public DateTime FromDate { get; set; } = DateTime.Today;
        public DateTime ToDate { get; set; } = DateTime.Today;
        public ReportType Type { get; set; }
        public int? EntityId { get; set; }
    }

    public enum ReportType
    {
        DailySales,
        MonthlySales,
        CustomerDebt,
        Pledges,
        Production,
        Inventory,
        RawMaterials,
        SupplierDebt,
        Salaries,
        Expenses,
        ProfitLoss,
        Invoice
    }

    public class InvoiceReportDto
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string DeliveryType { get; set; } = string.Empty;
        public decimal DeliveryCost { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public string? Notes { get; set; }
        public List<InvoiceItemDto> Items { get; set; } = new();
    }

    public class InvoiceItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class DailySalesReportDto
    {
        public DateTime ReportDate { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalRemaining { get; set; }
        public int CashOrders { get; set; }
        public int CreditOrders { get; set; }
        public int PledgeOrders { get; set; }
        public List<SalesOrderRowDto> Orders { get; set; } = new();
    }

    public class SalesOrderRowDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CustomerDebtReportDto
    {
        public DateTime ReportDate { get; set; }
        public decimal TotalDebt { get; set; }
        public int CustomersCount { get; set; }
        public List<CustomerDebtRowDto> Rows { get; set; } = new();
    }

    public class CustomerDebtRowDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public decimal TotalDebt { get; set; }
        public int UnpaidOrders { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }

    public class ProductionReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalProduced { get; set; }
        public int TotalDefective { get; set; }
        public int TotalNet { get; set; }
        public List<ProductionRowDto> Rows { get; set; } = new();
    }

    public class ProductionRowDto
    {
        public DateTime Date { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty;
        public int Produced { get; set; }
        public int Defective { get; set; }
        public int Net { get; set; }
    }

    public class InventoryReportDto
    {
        public DateTime ReportDate { get; set; }
        public int TotalProducts { get; set; }
        public int TotalBlocks { get; set; }
        public int LowStockCount { get; set; }
        public List<InventoryRowDto> Rows { get; set; } = new();
    }

    public class InventoryRowDto
    {
        public string ProductType { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int QuantityAvailable { get; set; }
        public int MinThreshold { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
