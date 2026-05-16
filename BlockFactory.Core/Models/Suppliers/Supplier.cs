using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Suppliers
{
    public enum SupplierType
    {
        Cement,      // إسمنت
        Sand,        // رمل
        Gravel,      // حصى
        Water,       // ماء
        Electricity, // كهرباء
        Other        // أخرى
    }

    public class Supplier : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public SupplierType SupplierType { get; set; }
        public decimal TotalDebt { get; set; } = 0;
        // ما على المصنع للمورد
        public string? Notes { get; set; }

        // Navigation
        public ICollection<SupplierInvoice> Invoices { get; set; }
            = new List<SupplierInvoice>();

        public ICollection<SupplierPayment> Payments { get; set; }
            = new List<SupplierPayment>();
    }
}
