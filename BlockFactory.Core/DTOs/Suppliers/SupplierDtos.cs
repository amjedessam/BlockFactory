using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.DTOs.Suppliers
{
    public class SupplierListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Phone { get; set; }
        public string SupplierType { get; set; } = string.Empty;
        public string TypeIcon { get; set; } = string.Empty;
        public decimal TotalDebt { get; set; }
        public string DebtStatusColor { get; set; } = "#27AE60";
        public int TotalInvoices { get; set; }
        public decimal TotalPurchases { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SupplierDetailDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string SupplierType { get; set; } = string.Empty;
        public decimal TotalDebt { get; set; }
        public string? Notes { get; set; }
        public List<SupplierInvoiceDto> RecentInvoices { get; set; }
            = new();
        public List<SupplierPaymentDto> RecentPayments { get; set; }
            = new();
    }

    public class SupplierInvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public List<SupplierInvoiceItemDto> Items { get; set; } = new();
    }

    public class SupplierInvoiceItemDto
    {
        public string? Description { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class SupplierPaymentDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Method { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateSupplierDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public Models.Suppliers.SupplierType SupplierType { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateSupplierInvoiceDto
    {
        public int SupplierId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Today;
        public DateTime? DueDate { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// true = آجل (يُسجّل كامل الإجمالي كدين)، false = دفع عند الإنشاء.
        /// </summary>
        public bool IsCredit { get; set; } = true;

        /// <summary>
        /// المبلغ المدفوع فوراً عند إنشاء الفاتورة (يُستخدم فقط إذا IsCredit = false).
        /// </summary>
        public decimal PayNowAmount { get; set; }

        public List<CreateSupplierInvoiceItemDto> Items { get; set; }
            = new();
    }

    public class CreateSupplierInvoiceItemDto
    {
        public int? RawMaterialId { get; set; }
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
    }

    public class PaySupplierDto
    {
        public int SupplierId { get; set; }
        public int? InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string? Method { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }

    public class SuppliersSummaryDto
    {
        public int TotalSuppliers { get; set; }
        public decimal TotalDebt { get; set; }
        public int SuppliersWithDebt { get; set; }
        public decimal TotalPurchasesThisMonth { get; set; }
    }

    /// <summary>قائمة مواد خام لاختيارها في فاتورة شراء.</summary>
    public class RawMaterialLookupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string UnitAr { get; set; } = string.Empty;
        public string DisplayText => $"{Name} — {UnitAr}";
    }
}
