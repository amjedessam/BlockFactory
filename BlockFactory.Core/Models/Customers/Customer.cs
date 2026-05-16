using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Sales;

namespace BlockFactory.Core.Models.Customers
{
    public class Customer : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }

        // الرصيد المستحق على العميل
        public decimal TotalDebt { get; set; } = 0;

        // Navigation
        public ICollection<Order> Orders { get; set; }
            = new List<Order>();

        public ICollection<Pledge> Pledges { get; set; }
            = new List<Pledge>();
    }
}
