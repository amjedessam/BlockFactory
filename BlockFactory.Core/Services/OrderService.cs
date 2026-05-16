using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.Orders;
using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Customers;
using BlockFactory.Core.Models.Finance;
using BlockFactory.Core.Models.Inventory;
using BlockFactory.Core.Models.Sales;
using BlockFactory.Core.Session;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Core.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAuthService _authService;
        private readonly IReportService _reportService;

        public OrderService(
            IUnitOfWork uow,
            IAuthService authService,
            IReportService reportService)
        {
            _uow = uow;
            _authService = authService;
            _reportService = reportService;
        }

        // ─── قائمة الطلبات ──────────────────────────
        public async Task<IEnumerable<OrderListDto>> GetAllOrdersAsync()
        {
            var orders = await _uow.Orders.Query()
                .Include(o => o.Customer)
                .Include(o => o.Pledge)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(MapToListDto);
        }

        public async Task<IEnumerable<OrderListDto>> GetOrdersByDateAsync(
            DateTime from, DateTime to)
        {
            var orders = await _uow.Orders.Query()
                .Include(o => o.Customer)
                .Include(o => o.Pledge)
                .Where(o => o.OrderDate.Date >= from.Date &&
                            o.OrderDate.Date <= to.Date)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(MapToListDto);
        }

        public async Task<IEnumerable<OrderListDto>> SearchOrdersAsync(
            string keyword)
        {
            keyword = keyword.Trim().ToLower();

            var orders = await _uow.Orders.Query()
                .Include(o => o.Customer)
                .Include(o => o.Pledge)
                .Where(o =>
                    o.OrderNumber.ToLower().Contains(keyword) ||
                    o.Customer!.FullName.ToLower().Contains(keyword) ||
                    (o.Customer.Phone != null &&
                     o.Customer.Phone.Contains(keyword)))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(MapToListDto);
        }

        public async Task<OrderDetailDto?> GetOrderDetailAsync(int orderId)
        {
            var order = await _uow.Orders
                .GetOrderWithDetailsAsync(orderId);

            if (order == null) return null;

            return new OrderDetailDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.FullName ?? "-",
                CustomerPhone = order.Customer?.Phone ?? "-",
                OrderDate = order.OrderDate,
                DueDate = order.DueDate,
                PaymentType = order.PaymentType,
                PaymentTypeName = GetPaymentTypeAr(order.PaymentType),
                PaymentStatus = order.PaymentStatus,
                DeliveryType = order.DeliveryType,
                DeliveryCost = order.DeliveryCost,
                SubTotal = order.SubTotal,
                Discount = order.Discount,
                TotalAmount = order.TotalAmount,
                PaidAmount = order.PaidAmount,
                RemainingAmount = order.RemainingAmount,
                ElectronicWalletName = order.ElectronicWalletName,
                TransactionReference = order.TransactionReference,
                Notes = order.Notes,

                Items = order.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "-",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList(),

                Payments = order.Payments.Select(p => new PaymentDto
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    Method = p.Method.ToString(),
                    WalletName = p.WalletName,
                    Reference = p.Reference,
                    Notes = p.Notes
                }).ToList(),

                Pledge = order.Pledge == null ? null : new PledgeDto
                {
                    Id = order.Pledge.Id,
                    Description = order.Pledge.Description,
                    PledgeType = GetPledgeTypeAr(order.Pledge.PledgeType),
                    Status = GetPledgeStatusAr(order.Pledge.Status),
                    DueDate = order.Pledge.DueDate,
                    Notes = order.Pledge.Notes
                }
            };
        }

        // ─── إنشاء طلب جديد ─────────────────────────
        public async Task<ServiceResult<int>> CreateOrderAsync(
            CreateOrderDto dto)
        {
            // التحقق من البيانات
            if (dto.Items.Count == 0)
                return ServiceResult<int>.Fail(
                    "يجب إضافة منتج واحد على الأقل");

            if (dto.CustomerId <= 0)
                return ServiceResult<int>.Fail("يجب اختيار عميل");

            // التحقق من الأسعار
            foreach (var item in dto.Items)
            {
                if (item.UnitPrice < item.PriceMin ||
                    item.UnitPrice > item.PriceMax)
                    return ServiceResult<int>.Fail(
                        $"سعر {item.ProductName} خارج النطاق المسموح " +
                        $"({item.PriceMin} - {item.PriceMax})");
            }

            await _uow.BeginTransactionAsync();

            try
            {
                // حساب الإجماليات
                var subTotal = dto.Items
                    .Sum(i => i.Quantity * i.UnitPrice);
                var totalAmount = subTotal
                    - dto.Discount
                    + dto.DeliveryCost;

                var initialPaid = dto.PaymentType == PaymentType.Cash
                    ? totalAmount
                    : dto.InitialPayment;

                var paymentStatus = initialPaid >= totalAmount
                    ? PaymentStatus.FullyPaid
                    : initialPaid > 0
                        ? PaymentStatus.PartiallyPaid
                        : PaymentStatus.Unpaid;

                // إنشاء الطلب
                var order = new Order
                {
                    OrderNumber = await _uow.Orders
                        .GenerateOrderNumberAsync(),
                    CustomerId = dto.CustomerId,
                    OrderDate = dto.OrderDate,
                    DueDate = dto.DueDate,
                    PaymentType = dto.PaymentType,
                    PaymentStatus = paymentStatus,
                    DeliveryType = dto.DeliveryType,
                    DeliveryCost = dto.DeliveryCost,
                    SubTotal = subTotal,
                    Discount = dto.Discount,
                    TotalAmount = totalAmount,
                    PaidAmount = initialPaid,
                    RemainingAmount = totalAmount - initialPaid,
                    ElectronicWalletName = dto.ElectronicWalletName,
                    TransactionReference = dto.TransactionReference,
                    Notes = dto.Notes,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = CurrentSession.Instance.UserId
                };

                await _uow.Orders.AddAsync(order);
                await _uow.SaveChangesAsync();

                // إضافة عناصر الطلب
                foreach (var item in dto.Items)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.Quantity * item.UnitPrice,
                        CreatedAt = DateTime.Now
                    };
                    await _uow.OrderItems.AddAsync(orderItem);
                    await UpdateInventoryAsync(
                        item.ProductId, item.Quantity, order.OrderNumber);
                }

                // إضافة الدفعة الأولية
                if (initialPaid > 0)
                {
                    await AddPaymentInternalAsync(
                        order.Id, initialPaid,
                        dto.PaymentType, dto.ElectronicWalletName,
                        dto.TransactionReference);
                }

                // تحديث دين العميل
                if (order.RemainingAmount > 0)
                {
                    await UpdateCustomerDebtAsync(
                        dto.CustomerId, order.RemainingAmount);
                }

                // إضافة الرهن إن وجد
                if (dto.Pledge != null)
                {
                    var pledge = new Pledge
                    {
                        CustomerId = dto.CustomerId,
                        OrderId = order.Id,
                        Description = dto.Pledge.Description,
                        PledgeType = dto.Pledge.PledgeType,
                        PledgeTypeOther = dto.Pledge.PledgeTypeOther,
                        DueDate = dto.Pledge.DueDate,
                        Status = PledgeStatus.Active,
                        Notes = dto.Pledge.Notes,
                        CreatedAt = DateTime.Now
                    };
                    await _uow.Pledges.AddAsync(pledge);
                }

                // إنشاء الفاتورة
                var invoice = new Invoice
                {
                    OrderId = order.Id,
                    InvoiceNumber = order.OrderNumber
                        .Replace("ORD", "INV"),
                    InvoiceDate = DateTime.Now,
                    CreatedAt = DateTime.Now
                };
                await _uow.Invoices.AddAsync(invoice);

                // قيد محاسبي
                await CreateSaleJournalEntryAsync(order);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                await _authService.LogActivityAsync(
                    "CreateOrder", "Order", order.Id,
                    newValues: $"رقم الطلب: {order.OrderNumber}");

                return ServiceResult<int>.Ok(order.Id,
                    $"تم إنشاء الطلب {order.OrderNumber} بنجاح");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult<int>.Fail(
                    $"خطأ في إنشاء الطلب: {ex.Message}");
            }
        }

        // ─── إضافة دفعة ─────────────────────────────
        public async Task<ServiceResult> AddPaymentAsync(AddPaymentDto dto)
        {
            var order = await _uow.Orders.GetByIdAsync(dto.OrderId);
            if (order == null)
                return ServiceResult.Fail("الطلب غير موجود");

            if (dto.Amount <= 0)
                return ServiceResult.Fail("المبلغ يجب أن يكون أكبر من صفر");

            if (dto.Amount > order.RemainingAmount)
                return ServiceResult.Fail(
                    $"المبلغ أكبر من المتبقي ({order.RemainingAmount:N0} ر.ي)");

            await _uow.BeginTransactionAsync();
            try
            {
                // إضافة الدفعة
                var payment = new Payment
                {
                    OrderId = dto.OrderId,
                    Amount = dto.Amount,
                    PaymentDate = DateTime.Now,
                    Method = dto.Method,
                    WalletName = dto.WalletName,
                    Reference = dto.Reference,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.Now
                };
                await _uow.Payments.AddAsync(payment);

                // تحديث الطلب
                order.PaidAmount += dto.Amount;
                order.RemainingAmount -= dto.Amount;
                order.PaymentStatus = order.RemainingAmount <= 0
                    ? PaymentStatus.FullyPaid
                    : PaymentStatus.PartiallyPaid;
                order.UpdatedAt = DateTime.Now;
                _uow.Orders.Update(order);

                // تحديث دين العميل
                await UpdateCustomerDebtAsync(
                    order.CustomerId, -dto.Amount);

                // تحديث رصيد المحفظة إن كان تحويل
                if (dto.Method == PaymentMethod.Electronic &&
                    !string.IsNullOrEmpty(dto.WalletName))
                {
                    await UpdateWalletAsync(dto.WalletName, dto.Amount);
                }

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                await _authService.LogActivityAsync(
                    "AddPayment", "Order", dto.OrderId,
                    newValues: $"دفعة: {dto.Amount:N0} ر.ي");

                return ServiceResult.Ok("تم تسجيل الدفعة بنجاح");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult.Fail($"خطأ: {ex.Message}");
            }
        }

        // ─── إلغاء طلب ──────────────────────────────
        public async Task<ServiceResult> CancelOrderAsync(
            int orderId, string reason)
        {
            var order = await _uow.Orders
                .GetOrderWithDetailsAsync(orderId);

            if (order == null)
                return ServiceResult.Fail("الطلب غير موجود");

            if (order.Status == OrderStatus.Cancelled)
                return ServiceResult.Fail("الطلب ملغى مسبقاً");

            await _uow.BeginTransactionAsync();
            try
            {
                // إرجاع المخزون
                foreach (var item in order.Items)
                {
                    await UpdateInventoryAsync(
                        item.ProductId, -item.Quantity,
                        order.OrderNumber, isReturn: true);
                }

                // إلغاء الطلب
                order.Status = OrderStatus.Cancelled;
                order.Notes = (order.Notes ?? "") +
                    $"\nإلغاء: {reason}";
                order.UpdatedAt = DateTime.Now;
                _uow.Orders.Update(order);

                // إرجاع الدين إن كان مسدداً جزئياً
                if (order.RemainingAmount > 0)
                    await UpdateCustomerDebtAsync(
                        order.CustomerId, -order.RemainingAmount);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                return ServiceResult.Ok("تم إلغاء الطلب بنجاح");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult.Fail($"خطأ: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateOrderStatusAsync(
            int orderId, string status)
        {
            var order = await _uow.Orders.GetByIdAsync(orderId);
            if (order == null)
                return ServiceResult.Fail("الطلب غير موجود");

            order.Status = status switch
            {
                "InProduction" => OrderStatus.InProduction,
                "Ready" => OrderStatus.Ready,
                "Delivered" => OrderStatus.Delivered,
                _ => order.Status
            };
            order.UpdatedAt = DateTime.Now;
            _uow.Orders.Update(order);
            await _uow.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        public Task<byte[]> GenerateInvoicePdfAsync(int orderId)
            => _reportService.GenerateInvoicePdfAsync(orderId);

        // ─── Private Helpers ────────────────────────

        private async Task UpdateInventoryAsync(
            int productId, int quantity,
            string reference, bool isReturn = false)
        {
            var stock = await _uow.Inventory
                .GetByProductAsync(productId);

            if (stock == null) return;

            stock.QuantityAvailable = isReturn
                ? stock.QuantityAvailable + quantity
                : stock.QuantityAvailable - quantity;
            stock.LastUpdated = DateTime.Now;

            _uow.Inventory.Update(stock);

            await _uow.ActivityLogs.AddAsync(
                new Models.Finance.ActivityLog
                {
                    Action = isReturn ? "ReturnStock" : "DeductStock",
                    EntityName = "Inventory",
                    EntityId = productId,
                    NewValues = $"الكمية: {quantity}، المرجع: {reference}",
                    UserId = CurrentSession.Instance.UserId,
                    LoggedAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                });
        }

        private async Task UpdateCustomerDebtAsync(
            int customerId, decimal amount)
        {
            var customer = await _uow.Customers
                .GetByIdAsync(customerId);
            if (customer == null) return;

            customer.TotalDebt += amount;
            if (customer.TotalDebt < 0)
                customer.TotalDebt = 0;

            customer.UpdatedAt = DateTime.Now;
            _uow.Customers.Update(customer);
        }

        private async Task UpdateWalletAsync(
            string walletName, decimal amount)
        {
            var wallet = await _uow.Wallets.Query()
                .FirstOrDefaultAsync(w =>
                    w.Name == walletName && w.IsActive);

            if (wallet == null) return;

            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.Now;
            _uow.Wallets.Update(wallet);
        }

        private async Task AddPaymentInternalAsync(
            int orderId, decimal amount,
            PaymentType paymentType,
            string? walletName,
            string? reference)
        {
            var method = paymentType == PaymentType.Electronic
                ? PaymentMethod.Electronic
                : PaymentMethod.Cash;

            var payment = new Payment
            {
                OrderId = orderId,
                Amount = amount,
                PaymentDate = DateTime.Now,
                Method = method,
                WalletName = walletName,
                Reference = reference,
                CreatedAt = DateTime.Now
            };
            await _uow.Payments.AddAsync(payment);

            if (method == PaymentMethod.Electronic &&
                !string.IsNullOrEmpty(walletName))
                await UpdateWalletAsync(walletName, amount);
        }

        private async Task CreateSaleJournalEntryAsync(Order order)
        {
            var entry = new JournalEntry
            {
                EntryNumber = $"JRN-{DateTime.Now:yyyyMMdd}-{order.Id}",
                EntryDate = DateTime.Now,
                Type = JournalEntryType.Sale,
                Description = $"مبيعات — {order.OrderNumber}",
                TotalDebit = order.TotalAmount,
                TotalCredit = order.TotalAmount,
                Reference = order.OrderNumber,
                IsPosted = true,
                CreatedAt = DateTime.Now
            };
            await _uow.JournalEntries.AddAsync(entry);
        }

        // ─── Mapping Helpers ─────────────────────────
        private static OrderListDto MapToListDto(
            Models.Sales.Order o) => new()
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.Customer?.FullName ?? "-",
                CustomerPhone = o.Customer?.Phone ?? "-",
                OrderDate = o.OrderDate,
                DueDate = o.DueDate,
                TotalAmount = o.TotalAmount,
                PaidAmount = o.PaidAmount,
                RemainingAmount = o.RemainingAmount,
                PaymentType = GetPaymentTypeAr(o.PaymentType),
                PaymentStatus = GetPaymentStatusAr(o.PaymentStatus),
                PaymentStatusColor = GetPaymentStatusColor(o.PaymentStatus),
                OrderStatus = GetOrderStatusAr(o.Status),
                DeliveryType = o.DeliveryType == DeliveryType.Delivery
                ? "توصيل" : "استلام",
                HasPledge = o.Pledge != null,
                IsOverdue = o.DueDate.HasValue &&
                        o.DueDate.Value.Date < DateTime.Today &&
                        o.PaymentStatus != PaymentStatus.FullyPaid
            };

        private static string GetPaymentTypeAr(PaymentType t) => t switch
        {
            PaymentType.Cash => "نقد",
            PaymentType.Electronic => "تحويل",
            PaymentType.Credit => "آجل",
            PaymentType.Pledge => "رهن",
            PaymentType.Mixed => "مختلط",
            _ => "-"
        };

        private static string GetPaymentStatusAr(PaymentStatus s) => s switch
        {
            PaymentStatus.FullyPaid => "مسدد",
            PaymentStatus.PartiallyPaid => "جزئي",
            PaymentStatus.Unpaid => "غير مسدد",
            _ => "-"
        };

        private static string GetPaymentStatusColor(PaymentStatus s) => s switch
        {
            PaymentStatus.FullyPaid => "#27AE60",
            PaymentStatus.PartiallyPaid => "#F39C12",
            PaymentStatus.Unpaid => "#E74C3C",
            _ => "#95A5A6"
        };

        private static string GetOrderStatusAr(OrderStatus s) => s switch
        {
            OrderStatus.Pending => "معلق",
            OrderStatus.InProduction => "قيد الإنتاج",
            OrderStatus.Ready => "جاهز",
            OrderStatus.Delivered => "تم التسليم",
            OrderStatus.Cancelled => "ملغي",
            _ => "-"
        };

        private static string GetPledgeTypeAr(
            Models.Customers.PledgeType t) => t switch
            {
                Models.Customers.PledgeType.Gold => "ذهب",
                Models.Customers.PledgeType.Weapon => "سلاح",
                Models.Customers.PledgeType.LandDeed => "ورقة أرض",
                Models.Customers.PledgeType.Other => "أخرى",
                _ => "-"
            };

        private static string GetPledgeStatusAr(
            PledgeStatus s) => s switch
            {
                PledgeStatus.Active => "نشط",
                PledgeStatus.Returned => "مُسترجع",
                PledgeStatus.Forfeited => "مصادر",
                _ => "-"
            };
    }
}
