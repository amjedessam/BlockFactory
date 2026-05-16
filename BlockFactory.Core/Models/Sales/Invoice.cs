using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Base;

namespace BlockFactory.Core.Models.Sales
{
    public class Invoice : BaseEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        // INV-2024-0001

        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public bool IsPrinted { get; set; } = false;
        public DateTime? PrintedAt { get; set; }

        // FK
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
    }
}
