// BlockFactory.Core/DTOs/Customers/CustomerDtos.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.DTOs.Customers
{
    public class CustomerListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public decimal TotalDebt { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalPurchases { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasActivePledge { get; set; }
        public string DebtStatusColor { get; set; } = "#27AE60";
    }

    public class CustomerDetailDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public decimal TotalDebt { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal TotalPaid { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CustomerOrderDto> RecentOrders { get; set; } = new();
        public List<CustomerPledgeDto> Pledges { get; set; } = new();
    }

    public class CustomerOrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }

    public class CustomerPledgeDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PledgeType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public string? RelatedOrderNumber { get; set; }
    }

    public class CreateCustomerDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateCustomerDto : CreateCustomerDto
    {
        public int Id { get; set; }
    }

    public class PledgeListDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PledgeType { get; set; } = string.Empty;
        public string PledgeTypeIcon { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string DueDateText { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
        public bool IsDueSoon { get; set; }
        public string? RelatedOrderNumber { get; set; }
        public decimal RelatedOrderAmount { get; set; }
    }

    public class ReturnPledgeDto
    {
        public int PledgeId { get; set; }
        public string? Notes { get; set; }
    }

    // ← هذا الـ class كان مفقوداً — يُستخدم في ICustomerService و NewOrderViewModel
    public class CustomerLookupDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public decimal TotalDebt { get; set; }
    }
}

/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.DTOs.Customers
{
    public class CustomerListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public decimal TotalDebt { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalPurchases { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasActivePledge { get; set; }
        public string DebtStatusColor { get; set; } = "#27AE60";
    }

    public class CustomerDetailDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public decimal TotalDebt { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal TotalPaid { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CustomerOrderDto> RecentOrders { get; set; } = new();
        public List<CustomerPledgeDto> Pledges { get; set; } = new();
    }

    public class CustomerOrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }

    public class CustomerPledgeDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PledgeType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public string? RelatedOrderNumber { get; set; }
    }

    public class CreateCustomerDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateCustomerDto : CreateCustomerDto
    {
        public int Id { get; set; }
    }

    public class PledgeListDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PledgeType { get; set; } = string.Empty;
        public string PledgeTypeIcon { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string DueDateText { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
        public bool IsDueSoon { get; set; }
        public string? RelatedOrderNumber { get; set; }
        public decimal RelatedOrderAmount { get; set; }
    }

    public class ReturnPledgeDto
    {
        public int PledgeId { get; set; }
        public string? Notes { get; set; }
    }
}*/
