using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.Customers;

using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Customers;
using BlockFactory.Core.Session;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Core.Services
{
    public class  CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAuthService _authService;

        public CustomerService(IUnitOfWork uow, IAuthService authService)
        {
            _uow = uow;
            _authService = authService;
        }

        // ─── قائمة العملاء ──────────────────────────
        public async Task<IEnumerable<CustomerListDto>> GetAllCustomersAsync()
        {
            var customers = await _uow.Customers.Query()
                .Include(c => c.Orders)
                .Include(c => c.Pledges)
                .OrderBy(c => c.FullName)
                .ToListAsync();

            return customers.Select(MapToListDto);
        }

        public async Task<IEnumerable<CustomerListDto>> SearchCustomersAsync(
            string keyword)
        {
            keyword = keyword.Trim().ToLower();

            var customers = await _uow.Customers.Query()
                .Include(c => c.Orders)
                .Include(c => c.Pledges)
                .Where(c =>
                    c.FullName.ToLower().Contains(keyword) ||
                    (c.Phone != null &&
                     c.Phone.Contains(keyword)))
                .OrderBy(c => c.FullName)
                .ToListAsync();

            return customers.Select(MapToListDto);
        }

        public async Task<IEnumerable<CustomerListDto>>
            GetCustomersWithDebtAsync()
        {
            var customers = await _uow.Customers.Query()
                .Include(c => c.Orders)
                .Include(c => c.Pledges)
                .Where(c => c.TotalDebt > 0 && !c.IsDeleted)
                .OrderByDescending(c => c.TotalDebt)
                .ToListAsync();

            return customers.Select(MapToListDto);
        }

        public async Task<CustomerDetailDto?> GetCustomerDetailAsync(
            int customerId)
        {
            var customer = await _uow.Customers.Query()
                .Include(c => c.Orders)
                    .ThenInclude(o => o.Items)
                        .ThenInclude(i => i.Product)
                .Include(c => c.Pledges)
                    .ThenInclude(p => p.Order)
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null) return null;

            return new CustomerDetailDto
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Address = customer.Address,
                Notes = customer.Notes,
                TotalDebt = customer.TotalDebt,
                TotalOrders = customer.Orders.Count,
                TotalPurchases = customer.Orders
                    .Sum(o => o.TotalAmount),
                TotalPaid = customer.Orders
                    .Sum(o => o.PaidAmount),
                CreatedAt = customer.CreatedAt,

                RecentOrders = customer.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .Select(o => new CustomerOrderDto
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount,
                        RemainingAmount = o.RemainingAmount,
                        PaymentStatus = GetPaymentStatusAr(o.PaymentStatus),
                        StatusColor = GetPaymentStatusColor(o.PaymentStatus)
                    }).ToList(),

                Pledges = customer.Pledges
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new CustomerPledgeDto
                    {
                        Id = p.Id,
                        Description = p.Description,
                        PledgeType = GetPledgeTypeAr(p.PledgeType),
                        Status = GetPledgeStatusAr(p.Status),
                        StatusColor = GetPledgeStatusColor(p.Status),
                        DueDate = p.DueDate,
                        IsOverdue = p.Status == PledgeStatus.Active &&
                                    p.DueDate.Date < DateTime.Today,
                        RelatedOrderNumber = p.Order?.OrderNumber
                    }).ToList()
            };
        }

        // ─── إنشاء عميل ─────────────────────────────
        public async Task<ServiceResult<int>> CreateCustomerAsync(
            CreateCustomerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                return ServiceResult<int>.Fail("اسم العميل مطلوب");

            // التحقق من التكرار
            bool exists = await _uow.Customers.AnyAsync(
                c => c.FullName == dto.FullName.Trim());

            if (exists)
                return ServiceResult<int>.Fail(
                    "يوجد عميل بنفس الاسم مسبقاً");

            var customer = new Customer
            {
                FullName = dto.FullName.Trim(),
                Phone = dto.Phone?.Trim(),
                Address = dto.Address?.Trim(),
                Notes = dto.Notes?.Trim(),
                TotalDebt = 0,
                CreatedAt = DateTime.Now,
                CreatedByUserId = CurrentSession.Instance.UserId
            };

            await _uow.Customers.AddAsync(customer);
            await _uow.SaveChangesAsync();

            await _authService.LogActivityAsync(
                "CreateCustomer", "Customer", customer.Id,
                newValues: customer.FullName);

            return ServiceResult<int>.Ok(customer.Id,
                "تم إضافة العميل بنجاح");
        }

        // ─── تحديث عميل ─────────────────────────────
        public async Task<ServiceResult> UpdateCustomerAsync(
            UpdateCustomerDto dto)
        {
            var customer = await _uow.Customers.GetByIdAsync(dto.Id);
            if (customer == null)
                return ServiceResult.Fail("العميل غير موجود");

            if (string.IsNullOrWhiteSpace(dto.FullName))
                return ServiceResult.Fail("اسم العميل مطلوب");

            var oldName = customer.FullName;
            customer.FullName = dto.FullName.Trim();
            customer.Phone = dto.Phone?.Trim();
            customer.Address = dto.Address?.Trim();
            customer.Notes = dto.Notes?.Trim();
            customer.UpdatedAt = DateTime.Now;

            _uow.Customers.Update(customer);
            await _uow.SaveChangesAsync();

            await _authService.LogActivityAsync(
                "UpdateCustomer", "Customer", dto.Id,
                oldValues: oldName,
                newValues: dto.FullName);

            return ServiceResult.Ok("تم تحديث بيانات العميل");
        }

        // ─── حذف عميل ───────────────────────────────
        public async Task<ServiceResult> DeleteCustomerAsync(int customerId)
        {
            var customer = await _uow.Customers.Query()
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null)
                return ServiceResult.Fail("العميل غير موجود");

            if (customer.Orders.Any())
                return ServiceResult.Fail(
                    "لا يمكن حذف عميل لديه طلبات مسجّلة");

            if (customer.TotalDebt > 0)
                return ServiceResult.Fail(
                    "لا يمكن حذف عميل لديه دين مستحق");

            customer.IsDeleted = true;
            customer.UpdatedAt = DateTime.Now;
            _uow.Customers.Update(customer);
            await _uow.SaveChangesAsync();

            return ServiceResult.Ok("تم حذف العميل");
        }

        // ─── الرهون ─────────────────────────────────
        public async Task<IEnumerable<PledgeListDto>> GetAllPledgesAsync()
        {
            var pledges = await _uow.Pledges.Query()
                .Include(p => p.Customer)
                .Include(p => p.Order)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return pledges.Select(MapToPledgeDto);
        }

        public async Task<IEnumerable<PledgeListDto>> GetActivePledgesAsync()
        {
            var pledges = await _uow.Pledges.Query()
                .Include(p => p.Customer)
                .Include(p => p.Order)
                .Where(p => p.Status == PledgeStatus.Active)
                .OrderBy(p => p.DueDate)
                .ToListAsync();

            return pledges.Select(MapToPledgeDto);
        }

        public async Task<IEnumerable<PledgeListDto>> GetOverduePledgesAsync()
        {
            var pledges = await _uow.Pledges.Query()
                .Include(p => p.Customer)
                .Include(p => p.Order)
                .Where(p =>
                    p.Status == PledgeStatus.Active &&
                    p.DueDate.Date < DateTime.Today)
                .OrderBy(p => p.DueDate)
                .ToListAsync();

            return pledges.Select(MapToPledgeDto);
        }

        public async Task<ServiceResult> ReturnPledgeAsync(
            ReturnPledgeDto dto)
        {
            var pledge = await _uow.Pledges.GetByIdAsync(dto.PledgeId);
            if (pledge == null)
                return ServiceResult.Fail("الرهن غير موجود");

            if (pledge.Status != PledgeStatus.Active)
                return ServiceResult.Fail("الرهن ليس نشطاً");

            pledge.Status = PledgeStatus.Returned;
            pledge.ReturnedAt = DateTime.Now;
            pledge.Notes = (pledge.Notes ?? "") +
                $"\nتم الاسترجاع: {dto.Notes}";
            pledge.UpdatedAt = DateTime.Now;

            _uow.Pledges.Update(pledge);
            await _uow.SaveChangesAsync();

            await _authService.LogActivityAsync(
                "ReturnPledge", "Pledge", dto.PledgeId);

            return ServiceResult.Ok("تم تسجيل استرجاع الرهن بنجاح");
        }

        // ─── Lookup ─────────────────────────────────
        public async Task<IEnumerable<CustomerLookupDto>>
            GetCustomerLookupsAsync(string keyword)
        {
            var customers = await _uow.Customers.Query()
                .Where(c =>
                    c.FullName.ToLower().Contains(keyword.ToLower()) ||
                    (c.Phone != null && c.Phone.Contains(keyword)))
                .Take(10)
                .ToListAsync();

            return customers.Select(c => new CustomerLookupDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Phone = c.Phone,
                TotalDebt = c.TotalDebt
            });
        }

        // ─── Helpers ────────────────────────────────
        private static CustomerListDto MapToListDto(
            Models.Customers.Customer c) => new()
            {
                Id = c.Id,
                FullName = c.FullName,
                Phone = c.Phone,
                Address = c.Address,
                TotalDebt = c.TotalDebt,
                TotalOrders = c.Orders?.Count ?? 0,
                TotalPurchases = c.Orders?.Sum(o => o.TotalAmount) ?? 0,
                CreatedAt = c.CreatedAt,
                HasActivePledge = c.Pledges?.Any(
                p => p.Status == PledgeStatus.Active) ?? false,
                DebtStatusColor = c.TotalDebt <= 0
                ? "#27AE60"
                : c.TotalDebt < 100000
                    ? "#F39C12"
                    : "#E74C3C"
            };

        private static PledgeListDto MapToPledgeDto(
            Models.Customers.Pledge p)
        {
            var today = DateTime.Today;
            var daysLeft = (p.DueDate.Date - today).Days;
            bool isOverdue = p.Status == PledgeStatus.Active &&
                             p.DueDate.Date < today;
            bool isDueSoon = p.Status == PledgeStatus.Active &&
                             daysLeft >= 0 && daysLeft <= 7;

            return new PledgeListDto
            {
                Id = p.Id,
                CustomerName = p.Customer?.FullName ?? "-",
                CustomerPhone = p.Customer?.Phone,
                Description = p.Description,
                PledgeType = GetPledgeTypeAr(p.PledgeType),
                PledgeTypeIcon = GetPledgeTypeIcon(p.PledgeType),
                Status = GetPledgeStatusAr(p.Status),
                StatusColor = GetPledgeStatusColor(p.Status),
                DueDate = p.DueDate,
                DueDateText = isOverdue
                    ? $"⚠️ متأخر {Math.Abs(daysLeft)} يوم"
                    : isDueSoon
                        ? $"🔔 بعد {daysLeft} أيام"
                        : p.DueDate.ToString("dd/MM/yyyy"),
                IsOverdue = isOverdue,
                IsDueSoon = isDueSoon,
                RelatedOrderNumber = p.Order?.OrderNumber,
                RelatedOrderAmount = p.Order?.TotalAmount ?? 0
            };
        }

        private static string GetPaymentStatusAr(
            Models.Sales.PaymentStatus s) => s switch
            {
                Models.Sales.PaymentStatus.FullyPaid => "مسدد",
                Models.Sales.PaymentStatus.PartiallyPaid => "جزئي",
                Models.Sales.PaymentStatus.Unpaid => "غير مسدد",
                _ => "-"
            };

        private static string GetPaymentStatusColor(
            Models.Sales.PaymentStatus s) => s switch
            {
                Models.Sales.PaymentStatus.FullyPaid => "#27AE60",
                Models.Sales.PaymentStatus.PartiallyPaid => "#F39C12",
                Models.Sales.PaymentStatus.Unpaid => "#E74C3C",
                _ => "#95A5A6"
            };

        private static string GetPledgeTypeAr(PledgeType t) => t switch
        {
            PledgeType.Gold => "ذهب",
            PledgeType.Weapon => "سلاح",
            PledgeType.LandDeed => "ورقة أرض",
            PledgeType.Other => "أخرى",
            _ => "-"
        };

        private static string GetPledgeTypeIcon(PledgeType t) => t switch
        {
            PledgeType.Gold => "💰",
            PledgeType.Weapon => "🔫",
            PledgeType.LandDeed => "📜",
            PledgeType.Other => "📦",
            _ => "❓"
        };

        private static string GetPledgeStatusAr(PledgeStatus s) => s switch
        {
            PledgeStatus.Active => "نشط",
            PledgeStatus.Returned => "مُسترجع",
            PledgeStatus.Forfeited => "مصادر",
            _ => "-"
        };

        private static string GetPledgeStatusColor(PledgeStatus s) => s switch
        {
            PledgeStatus.Active => "#E67E22",
            PledgeStatus.Returned => "#27AE60",
            PledgeStatus.Forfeited => "#E74C3C",
            _ => "#95A5A6"
        };
    }
}
