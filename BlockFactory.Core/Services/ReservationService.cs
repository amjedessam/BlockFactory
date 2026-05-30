// BlockFactory.Core/Services/ReservationService.cs

// BlockFactory.Core/Services/ReservationService.cs

using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.Reservations;
using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Inventory;
using BlockFactory.Core.Models.Reservations;
using BlockFactory.Core.Models.Sales;
using BlockFactory.Core.Session;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Core.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IUnitOfWork _uow;

        public ReservationService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ════════════════════════════════════════════
        // إنشاء الحجز
        // ════════════════════════════════════════════

        public async Task<ServiceResult<int>> CreateReservationAsync(
            CreateReservationDto dto)
        {
            // ── التحقق الأساسي ──
            if (dto.AmountPaid <= 0)
                return ServiceResult<int>.Fail("المبلغ المدفوع يجب أن يكون أكبر من صفر");

            var customer = await _uow.Customers.GetByIdAsync(dto.CustomerId);
            if (customer == null)
                return ServiceResult<int>.Fail("العميل غير موجود");

            // ── التحقق الخاص بالحجز المحدد ──
            if (dto.Type == ReservationType.QuantityReservation)
            {
                if (!dto.Items.Any())
                    return ServiceResult<int>.Fail(
                        "الحجز المحدد يتطلب تحديد نوع وكمية البلك");

                // التحقق من توافر الكميات
                foreach (var item in dto.Items)
                {
                    var stock = await _uow.Inventory.GetByProductAsync(item.ProductId);
                    if (stock == null)
                        return ServiceResult<int>.Fail(
                            $"لم يُعثر على مخزون للمنتج ID={item.ProductId}");

                    if (!dto.SkipStockValidation && stock.FreeQuantity < item.Quantity)
                        return ServiceResult<int>.Fail(
                            $"الكمية المطلوبة ({item.Quantity:N0}) " +
                            $"تتجاوز المتاح للحجز ({stock.FreeQuantity:N0}) " +
                            $"للمنتج: {stock.Product?.Name}");
                }
            }

            await _uow.BeginTransactionAsync();
            try
            {
                // ── إنشاء رقم الفاتورة ──
                var reservationNumber = await GenerateReservationNumberAsync();

                // ── إنشاء الحجز ──
                var reservation = new Reservation
                {
                    ReservationNumber = reservationNumber,
                    Type = dto.Type,
                    Status = ReservationStatus.Active,
                    CustomerId = dto.CustomerId,
                    AmountPaid = dto.AmountPaid,
                    AmountUsed = 0,
                    PaymentMethod = dto.PaymentMethod,
                    WalletName = dto.WalletName,
                    TransactionReference = dto.TransactionReference,
                    ReservationDate = dto.ReservationDate,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = CurrentSession.Instance.UserId
                };

                await _uow.Reservations.AddAsync(reservation);
                await _uow.SaveChangesAsync(); // نحصل على reservation.Id

                // ── حفظ أصناف الحجز المحدد + تحديث المخزون ──
                if (dto.Type == ReservationType.QuantityReservation)
                {
                    foreach (var item in dto.Items)
                    {
                        var reservationItem = new ReservationItem
                        {
                            ReservationId = reservation.Id,
                            ProductId = item.ProductId,
                            QuantityReserved = item.Quantity,
                            QuantityWithdrawn = 0,
                            UnitPrice = item.UnitPrice,
                            CreatedAt = DateTime.Now
                        };
                        await _uow.ReservationItems.AddAsync(reservationItem);

                        // تحديث المخزون: Available → Reserved
                        var stock = await _uow.Inventory.GetByProductAsync(item.ProductId);
                        if (stock != null)
                        {
                            stock.ReservedQuantity += item.Quantity;
                            stock.LastUpdated = DateTime.Now;
                            _uow.Inventory.Update(stock);
                        }
                    }
                    await _uow.SaveChangesAsync();
                }

                // ── حفظ Snapshot كامل للكتالوج ──
                await SavePriceSnapshotAsync(reservation.Id);

                await _uow.CommitAsync();

                return ServiceResult<int>.Ok(
                    reservation.Id,
                    $"تم إنشاء فاتورة الحجز {reservationNumber} بنجاح");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult<int>.Fail($"خطأ أثناء إنشاء الحجز: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════
        // السحب
        // ════════════════════════════════════════════

        public async Task<ServiceResult<int>> CreateWithdrawalAsync(
            CreateWithdrawalDto dto)
        {
            var reservation = await _uow.Reservations
                .Query()
                .Include(r => r.Items)
                    .ThenInclude(i => i.Product)
                .Include(r => r.PriceSnapshots)
                .FirstOrDefaultAsync(r => r.Id == dto.ReservationId
                                       && !r.IsDeleted);

            if (reservation == null)
                return ServiceResult<int>.Fail("فاتورة الحجز غير موجودة");

            if (reservation.Status == ReservationStatus.FullyUsed)
                return ServiceResult<int>.Fail("هذا الحجز مستهلك بالكامل");

            if (reservation.Status == ReservationStatus.Cancelled)
                return ServiceResult<int>.Fail("هذا الحجز ملغي");

            if (!dto.Items.Any())
                return ServiceResult<int>.Fail("يجب تحديد صنف واحد على الأقل للسحب");

            await _uow.BeginTransactionAsync();
            try
            {
                decimal totalAmount = 0;
                var withdrawalItems = new List<WithdrawalItem>();

                foreach (var itemDto in dto.Items)
                {
                    // جلب السعر من Snapshot
                    var snapshot = reservation.PriceSnapshots
                        .FirstOrDefault(s => s.ProductId == itemDto.ProductId);

                    if (snapshot == null)
                        return ServiceResult<int>.Fail(
                            $"لا يوجد سعر محدد للمنتج ID={itemDto.ProductId} في هذا الحجز");

                    var product = await _uow.Products.GetByIdAsync(itemDto.ProductId);

                    // ── التحقق حسب نوع الحجز ──
                    if (reservation.Type == ReservationType.QuantityReservation)
                    {
                        var reservedItem = reservation.Items
                            .FirstOrDefault(i => i.ProductId == itemDto.ProductId);

                        if (reservedItem == null)
                            return ServiceResult<int>.Fail(
                                $"المنتج {snapshot.ProductName} غير موجود في قائمة هذا الحجز المحدد");

                        if (itemDto.Quantity > reservedItem.QuantityRemaining)
                            return ServiceResult<int>.Fail(
                                $"الكمية المطلوبة ({itemDto.Quantity:N0}) تتجاوز " +
                                $"المتبقي المحجوز ({reservedItem.QuantityRemaining:N0}) " +
                                $"للمنتج: {snapshot.ProductName}");

                        // تحديث QuantityWithdrawn في ReservationItem
                        reservedItem.QuantityWithdrawn += itemDto.Quantity;
                        _uow.ReservationItems.Update(reservedItem);

                        // تحديث المخزون: ReservedQuantity يتناقص → تحويل Delivered
                        var stock = await _uow.Inventory.GetByProductAsync(itemDto.ProductId);
                        if (stock != null)
                        {
                            stock.ReservedQuantity = Math.Max(0,
                                stock.ReservedQuantity - itemDto.Quantity);
                            stock.QuantityAvailable = Math.Max(0,
                                stock.QuantityAvailable - itemDto.Quantity);
                            stock.LastUpdated = DateTime.Now;
                            _uow.Inventory.Update(stock);
                        }
                    }
                    else // OpenBalance
                    {
                        // للحجز المفتوح — التحقق يتم على الرصيد الكلي بعد حساب الكل
                        // المخزون لا يتأثر الآن (يتأثر عند التسليم الفعلي خارج النظام)
                    }

                    decimal itemTotal = itemDto.Quantity * snapshot.Price;
                    totalAmount += itemTotal;

                    withdrawalItems.Add(new WithdrawalItem
                    {
                        ProductId = itemDto.ProductId,
                        ProductName = snapshot.ProductName,
                        Quantity = itemDto.Quantity,
                        UnitPrice = snapshot.Price,
                        CreatedAt = DateTime.Now
                    });
                }

                // التحقق النهائي للحجز المفتوح
                if (reservation.Type == ReservationType.OpenBalance)
                {
                    if (totalAmount > reservation.AmountRemaining)
                        return ServiceResult<int>.Fail(
                            $"إجمالي السحب ({totalAmount:N0} ر.ي) يتجاوز " +
                            $"الرصيد المتبقي ({reservation.AmountRemaining:N0} ر.ي)");
                }

                // ── إنشاء سجل السحب ──
                var withdrawalNumber = await GenerateWithdrawalNumberAsync();

                var withdrawal = new WithdrawalTransaction
                {
                    ReservationId = reservation.Id,
                    WithdrawalNumber = withdrawalNumber,
                    WithdrawalDate = dto.WithdrawalDate,
                    Status = WithdrawalStatus.Completed,
                    TotalAmount = totalAmount,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = CurrentSession.Instance.UserId
                };

                await _uow.WithdrawalTransactions.AddAsync(withdrawal);
                await _uow.SaveChangesAsync();

                // ربط أصناف السحب بالسجل
                foreach (var item in withdrawalItems)
                {
                    item.WithdrawalTransactionId = withdrawal.Id;
                    await _uow.WithdrawalItems.AddAsync(item);
                }

                // تحديث الحجز
                reservation.AmountUsed += totalAmount;
                reservation.Status = reservation.AmountRemaining <= 0
                    ? ReservationStatus.FullyUsed
                    : ReservationStatus.PartiallyUsed;
                reservation.UpdatedAt = DateTime.Now;
                _uow.Reservations.Update(reservation);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                return ServiceResult<int>.Ok(
                    withdrawal.Id,
                    $"تم تنفيذ السحب {withdrawalNumber} بقيمة {totalAmount:N0} ر.ي");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult<int>.Fail($"خطأ أثناء تنفيذ السحب: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════
        // إلغاء الحجز
        // ════════════════════════════════════════════

        public async Task<ServiceResult<decimal>> CancelReservationAsync(
            int reservationId, string? cancellationNotes = null)
        {
            var reservation = await _uow.Reservations
                .Query()
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == reservationId && !r.IsDeleted);

            if (reservation == null)
                return ServiceResult<decimal>.Fail("فاتورة الحجز غير موجودة");

            if (reservation.Status == ReservationStatus.Cancelled)
                return ServiceResult<decimal>.Fail("هذا الحجز ملغي مسبقاً");

            if (reservation.Status == ReservationStatus.FullyUsed)
                return ServiceResult<decimal>.Fail(
                    "هذا الحجز مستهلك بالكامل — لا يوجد رصيد للإلغاء");

            var refundAmount = reservation.AmountRemaining;

            await _uow.BeginTransactionAsync();
            try
            {
                // للحجز المحدد: تحرير الكميات المحجوزة المتبقية
                if (reservation.Type == ReservationType.QuantityReservation)
                {
                    foreach (var item in reservation.Items)
                    {
                        int qtyToRelease = item.QuantityRemaining;
                        if (qtyToRelease <= 0) continue;

                        var stock = await _uow.Inventory
                            .GetByProductAsync(item.ProductId);
                        if (stock != null)
                        {
                            stock.ReservedQuantity = Math.Max(0,
                                stock.ReservedQuantity - qtyToRelease);
                            stock.LastUpdated = DateTime.Now;
                            _uow.Inventory.Update(stock);
                        }
                    }
                }

                reservation.Status = ReservationStatus.Cancelled;
                reservation.CancellationDate = DateTime.Now;
                reservation.RefundedAmount = refundAmount;
                reservation.Notes = string.IsNullOrEmpty(cancellationNotes)
                    ? reservation.Notes
                    : $"{reservation.Notes}\n[إلغاء]: {cancellationNotes}";
                reservation.UpdatedAt = DateTime.Now;

                _uow.Reservations.Update(reservation);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                return ServiceResult<decimal>.Ok(
                    refundAmount,
                    $"تم إلغاء الحجز. المبلغ المسترد: {refundAmount:N0} ر.ي");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult<decimal>.Fail($"خطأ أثناء الإلغاء: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════
        // الاستعلام
        // ════════════════════════════════════════════

        public async Task<ReservationDetailDto?> GetReservationByIdAsync(
            int reservationId)
        {
            var r = await _uow.Reservations
                .Query()
                .Include(r => r.Customer)
                .Include(r => r.Items).ThenInclude(i => i.Product)
                .Include(r => r.PriceSnapshots).ThenInclude(s => s.Product)
                .Include(r => r.Withdrawals).ThenInclude(w => w.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(r => r.Id == reservationId && !r.IsDeleted);

            return r == null ? null : MapToDetail(r);
        }

        public async Task<IEnumerable<ReservationListDto>> GetAllActiveAsync()
        {
            var list = await _uow.Reservations
                .Query()
                .Include(r => r.Customer)
                .Include(r => r.Withdrawals)
                .Where(r => r.Status == ReservationStatus.Active
                         || r.Status == ReservationStatus.PartiallyUsed)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            return list.Select(MapToList);
        }

        public async Task<IEnumerable<ReservationListDto>> GetByCustomerAsync(
            int customerId)
        {
            var list = await _uow.Reservations
                .Query()
                .Include(r => r.Customer)
                .Include(r => r.Withdrawals)
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            return list.Select(MapToList);
        }

        public async Task<CustomerReservationSummaryDto> GetCustomerSummaryAsync(
            int customerId)
        {
            var active = await _uow.Reservations
                .Query()
                .Include(r => r.Customer)
                .Include(r => r.Items)
                .Include(r => r.Withdrawals)
                .Where(r => r.CustomerId == customerId
                         && (r.Status == ReservationStatus.Active
                          || r.Status == ReservationStatus.PartiallyUsed))
                .ToListAsync();

            return new CustomerReservationSummaryDto
            {
                ActiveReservationsCount = active.Count,
                TotalRemainingBalance = active.Sum(r => r.AmountRemaining),
                TotalQuantityRemaining = active
                    .Where(r => r.Type == ReservationType.QuantityReservation)
                    .SelectMany(r => r.Items)
                    .Sum(i => i.QuantityRemaining),
                ActiveReservations = active.Select(MapToList).ToList()
            };
        }

        public async Task<IEnumerable<ReservationListDto>> SearchAsync(string keyword)
        {
            var list = await _uow.Reservations
                .Query()
                .Include(r => r.Customer)
                .Include(r => r.Withdrawals)
                .Where(r => r.ReservationNumber.Contains(keyword)
                         || r.Customer.FullName.Contains(keyword))
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            return list.Select(MapToList);
        }

        public async Task<WithdrawalListDto?> GetWithdrawalByIdAsync(int withdrawalId)
        {
            var w = await _uow.WithdrawalTransactions
                .Query()
                .Include(w => w.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(w => w.Id == withdrawalId);

            return w == null ? null : MapWithdrawal(w);
        }

        public async Task<IEnumerable<WithdrawalListDto>> GetWithdrawalsByReservationAsync(
            int reservationId)
        {
            var list = await _uow.WithdrawalTransactions
                .Query()
                .Include(w => w.Items).ThenInclude(i => i.Product)
                .Where(w => w.ReservationId == reservationId)
                .OrderByDescending(w => w.WithdrawalDate)
                .ToListAsync();

            return list.Select(MapWithdrawal);
        }

        public async Task<IEnumerable<PriceSnapshotDto>> GetPriceSnapshotAsync(
            int reservationId)
        {
            var snapshots = await _uow.PriceSnapshots
                .Query()
                .Include(s => s.Product)
                .Where(s => s.ReservationId == reservationId)
                .OrderBy(s => s.ProductName)
                .ToListAsync();

            return snapshots.Select(s => new PriceSnapshotDto
            {
                ProductId = s.ProductId,
                ProductName = s.ProductName,
                Price = s.Price,
                PriceMin = s.PriceMin,
                PriceMax = s.PriceMax
            });
        }

        // ════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════

        private async Task SavePriceSnapshotAsync(int reservationId)
        {
            var products = await _uow.Products
                .Query()
                .Include(p => p.ProductType)
                .Where(p => p.IsActive && !p.IsDeleted)
                .ToListAsync();

            foreach (var product in products)
            {
                var snapshot = new PriceSnapshot
                {
                    ReservationId = reservationId,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.DefaultPrice,
                    PriceMin = product.PriceMin,
                    PriceMax = product.PriceMax,
                    SnapshotDate = DateTime.Now,
                    CreatedAt = DateTime.Now
                };
                await _uow.PriceSnapshots.AddAsync(snapshot);
            }
            await _uow.SaveChangesAsync();
        }

        private async Task<string> GenerateReservationNumberAsync()
        {
            var today = DateTime.Today;
            var prefix = $"RES-{today:yyyyMMdd}-";
            var count = await _uow.Reservations
                .Query()
                .CountAsync(r => r.ReservationNumber.StartsWith(prefix));
            return $"{prefix}{(count + 1):D3}";
        }

        private async Task<string> GenerateWithdrawalNumberAsync()
        {
            var today = DateTime.Today;
            var prefix = $"WD-{today:yyyyMMdd}-";
            var count = await _uow.WithdrawalTransactions
                .Query()
                .CountAsync(w => w.WithdrawalNumber.StartsWith(prefix));
            return $"{prefix}{(count + 1):D3}";
        }

        // ─── Mappers ────────────────────────────────

        private static ReservationListDto MapToList(Reservation r)
        {
            var (typeText, typeColor) = r.Type == ReservationType.QuantityReservation
                ? ("حجز محدد", "#2980B9")
                : ("حجز مفتوح", "#8E44AD");

            var (statusText, statusColor) = r.Status switch
            {
                ReservationStatus.Active => ("نشط", "#27AE60"),
                ReservationStatus.PartiallyUsed => ("مستهلك جزئياً", "#F39C12"),
                ReservationStatus.FullyUsed => ("مستهلك كلياً", "#7F8C8D"),
                ReservationStatus.Cancelled => ("ملغي", "#E74C3C"),
                _ => ("غير معروف", "#95A5A6")
            };

            return new ReservationListDto
            {
                Id = r.Id,
                ReservationNumber = r.ReservationNumber,
                CustomerName = r.Customer?.FullName ?? string.Empty,
                CustomerPhone = r.Customer?.Phone,
                TypeText = typeText,
                TypeColor = typeColor,
                StatusText = statusText,
                StatusColor = statusColor,
                AmountPaid = r.AmountPaid,
                AmountUsed = r.AmountUsed,
                AmountRemaining = r.AmountRemaining,
                ReservationDate = r.ReservationDate,
                WithdrawalsCount = r.Withdrawals?.Count ?? 0
            };
        }

        private static ReservationDetailDto MapToDetail(Reservation r)
        {
            var list = MapToList(r);
            return new ReservationDetailDto
            {
                Id = r.Id,
                ReservationNumber = r.ReservationNumber,
                CustomerId = r.CustomerId,
                CustomerName = list.CustomerName,
                CustomerPhone = list.CustomerPhone,
                Type = r.Type,
                TypeText = list.TypeText,
                Status = r.Status,
                StatusText = list.StatusText,
                StatusColor = list.StatusColor,
                AmountPaid = r.AmountPaid,
                AmountUsed = r.AmountUsed,
                AmountRemaining = r.AmountRemaining,
                PaymentMethodText = r.PaymentMethod.ToString(),
                WalletName = r.WalletName,
                TransactionReference = r.TransactionReference,
                ReservationDate = r.ReservationDate,
                Notes = r.Notes,
                Items = r.Items.Select(i => new ReservationItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? string.Empty,
                    QuantityReserved = i.QuantityReserved,
                    QuantityWithdrawn = i.QuantityWithdrawn,
                    QuantityRemaining = i.QuantityRemaining,
                    UnitPrice = i.UnitPrice,
                    TotalAmount = i.TotalAmount,
                    StatusColor = i.QuantityRemaining == 0 ? "#7F8C8D" : "#27AE60"
                }).ToList(),
                PriceSnapshots = r.PriceSnapshots.Select(s => new PriceSnapshotDto
                {
                    ProductId = s.ProductId,
                    ProductName = s.ProductName,
                    Price = s.Price,
                    PriceMin = s.PriceMin,
                    PriceMax = s.PriceMax
                }).ToList(),
                Withdrawals = r.Withdrawals.Select(MapWithdrawal).ToList()
            };
        }

        // ════════════════════════════════════════════
        // توليد الفواتير PDF
        // ════════════════════════════════════════════

        public async Task<byte[]?> GenerateReservationInvoicePdfAsync(int reservationId)
        {
            // جلب بيانات الحجز كاملة
            var reservation = await _uow.Reservations
                .Query()
                .Include(r => r.Customer)
                .Include(r => r.Items).ThenInclude(i => i.Product)
                .Include(r => r.Withdrawals)
                .FirstOrDefaultAsync(r => r.Id == reservationId && !r.IsDeleted);

            if (reservation == null) return null;

            try
            {
                // ── بناء الـ PDF بدون مكتبة خارجية (HTML → bytes) ──────
                // إذا كان لديك QuestPDF أو iTextSharp يمكن استبدال هذا القسم.
                // هنا نولّد HTML بسيطاً ثم نحوّله لـ bytes لفتحه بالمتصفح.

                var html = BuildReservationHtml(reservation);
                var bytes = System.Text.Encoding.UTF8.GetBytes(html);

                // حفظ كـ .html مؤقتاً — سيُفتح بالمتصفح الافتراضي
                // لاستبدالها بـ PDF حقيقي أضف: Install-Package QuestPDF
                return await Task.FromResult(bytes);
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]?> GenerateWithdrawalInvoicePdfAsync(int withdrawalId)
        {
            var withdrawal = await _uow.WithdrawalTransactions
                .Query()
                .Include(w => w.Reservation).ThenInclude(r => r.Customer)
                .Include(w => w.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(w => w.Id == withdrawalId);

            if (withdrawal == null) return null;

            try
            {
                var html = BuildWithdrawalHtml(withdrawal);
                var bytes = System.Text.Encoding.UTF8.GetBytes(html);
                return await Task.FromResult(bytes);
            }
            catch
            {
                return null;
            }
        }

        // ─── HTML builders ───────────────────────────

        private static string BuildReservationHtml(Reservation r)
        {
            var typeText = r.Type == ReservationType.QuantityReservation
                ? "حجز محدد" : "حجز مفتوح";

            var paymentText = r.PaymentMethod == PaymentMethod.Cash
                ? "نقد" : "تحويل إلكتروني";

            // ── صفوف الأصناف ──
            var itemsRows = new System.Text.StringBuilder();
            if (r.Type == ReservationType.QuantityReservation)
            {
                foreach (var i in r.Items)
                {
                    itemsRows.Append("<tr>");
                    itemsRows.Append("<td>" + (i.Product?.Name ?? "") + "</td>");
                    itemsRows.Append("<td>" + i.QuantityReserved.ToString("N0") + "</td>");
                    itemsRows.Append("<td>" + i.UnitPrice.ToString("N0") + " ر.ي</td>");
                    itemsRows.Append("<td>" + i.TotalAmount.ToString("N0") + " ر.ي</td>");
                    itemsRows.Append("</tr>");
                }
            }

            var itemsTableHtml = r.Type == ReservationType.QuantityReservation
                ? "<table>" +
                  "<thead><tr><th>المنتج</th><th>الكمية</th><th>السعر</th><th>الإجمالي</th></tr></thead>" +
                  "<tbody>" + itemsRows + "</tbody>" +
                  "</table>"
                : "";

            var walletRow = r.WalletName != null
                ? "<div class=\"info-row\"><span class=\"label\">المحفظة:</span><span class=\"value\">" + r.WalletName + "</span></div>"
                : "";

            var notesRow = r.Notes != null
                ? "<div class=\"info-row\"><span class=\"label\">ملاحظات:</span><span class=\"value\">" + r.Notes + "</span></div>"
                : "";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html dir=\"rtl\" lang=\"ar\">");
            sb.AppendLine("<head><meta charset=\"UTF-8\"/>");
            sb.AppendLine("<title>فاتورة حجز " + r.ReservationNumber + "</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:'Segoe UI',Tahoma,Arial;direction:rtl;margin:30px;color:#222;font-size:14px;}");
            sb.AppendLine(".header{text-align:center;border-bottom:2px solid #1E3A5F;padding-bottom:16px;margin-bottom:20px;}");
            sb.AppendLine(".header h1{color:#1E3A5F;margin:0;font-size:22px;}");
            sb.AppendLine(".header h2{color:#555;margin:4px 0 0;font-size:15px;}");
            sb.AppendLine(".badge{display:inline-block;padding:4px 14px;border-radius:20px;background:#EBF5FB;color:#2980B9;font-weight:bold;font-size:13px;}");
            sb.AppendLine(".info-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px 30px;margin-bottom:20px;}");
            sb.AppendLine(".info-row{display:flex;gap:8px;}");
            sb.AppendLine(".label{color:#555;min-width:120px;}");
            sb.AppendLine(".value{font-weight:600;}");
            sb.AppendLine("table{width:100%;border-collapse:collapse;margin-top:10px;}");
            sb.AppendLine("th{background:#1E3A5F;color:white;padding:10px;text-align:right;}");
            sb.AppendLine("td{padding:9px 10px;border-bottom:1px solid #eee;}");
            sb.AppendLine("tr:nth-child(even) td{background:#F8F9FA;}");
            sb.AppendLine(".total-row{font-size:18px;font-weight:bold;color:#1E3A5F;margin-top:16px;text-align:left;}");
            sb.AppendLine(".footer{margin-top:30px;text-align:center;color:#999;font-size:12px;border-top:1px solid #eee;padding-top:12px;}");
            sb.AppendLine("@media print{button{display:none;}}");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<div class=\"header\">");
            sb.AppendLine("<h1> مصنع البلوك</h1>");
            sb.AppendLine("<h2>فاتورة " + typeText + "</h2>");
            sb.AppendLine("<span class=\"badge\">" + r.ReservationNumber + "</span>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"info-grid\">");
            sb.AppendLine("<div class=\"info-row\"><span class=\"label\">العميل:</span><span class=\"value\">" + (r.Customer?.FullName ?? "") + "</span></div>");
            sb.AppendLine("<div class=\"info-row\"><span class=\"label\">رقم الهاتف:</span><span class=\"value\">" + (r.Customer?.Phone ?? "—") + "</span></div>");
            sb.AppendLine("<div class=\"info-row\"><span class=\"label\">تاريخ الفاتورة:</span><span class=\"value\">" + r.ReservationDate.ToString("dd/MM/yyyy") + "</span></div>");
            sb.AppendLine("<div class=\"info-row\"><span class=\"label\">نوع الحجز:</span><span class=\"value\">" + typeText + "</span></div>");
            sb.AppendLine("<div class=\"info-row\"><span class=\"label\">طريقة الدفع:</span><span class=\"value\">" + paymentText + "</span></div>");
            sb.AppendLine(walletRow);
            sb.AppendLine(notesRow);
            sb.AppendLine("</div>");
            sb.AppendLine(itemsTableHtml);
            sb.AppendLine("<div class=\"total-row\"> المبلغ المدفوع: " + r.AmountPaid.ToString("N0") + " ر.ي</div>");
            sb.AppendLine("<div class=\"footer\">نظام إدارة مصنع البلوك — تم الإصدار: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "</div>");
            sb.AppendLine("<script>window.onload=function(){window.print();}</script>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string BuildWithdrawalHtml(WithdrawalTransaction w)
        {
            var itemsRows = new System.Text.StringBuilder();
            foreach (var i in w.Items)
            {
                itemsRows.Append("<tr>");
                itemsRows.Append("<td>" + i.ProductName + "</td>");
                itemsRows.Append("<td>" + i.Quantity.ToString("N0") + "</td>");
                itemsRows.Append("<td>" + i.UnitPrice.ToString("N0") + " ر.ي</td>");
                itemsRows.Append("<td>" + i.TotalAmount.ToString("N0") + " ر.ي</td>");
                itemsRows.Append("</tr>");
            }

            var notesRow = w.Notes != null
                ? "<div class=\"info-row\"><span class=\"label\">ملاحظات:</span><span class=\"value\">" + w.Notes + "</span></div>"
                : "";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html dir=\"rtl\" lang=\"ar\">");
            sb.AppendLine("<head><meta charset=\"UTF-8\"/>");
            sb.AppendLine("<title>فاتورة سحب " + w.WithdrawalNumber + "</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:'Segoe UI',Tahoma,Arial;direction:rtl;margin:30px;color:#222;font-size:14px;}");
            sb.AppendLine(".header{text-align:center;border-bottom:2px solid #27AE60;padding-bottom:16px;margin-bottom:20px;}");
            sb.AppendLine(".header h1{color:#1E3A5F;margin:0;font-size:22px;}");
            sb.AppendLine(".header h2{color:#555;margin:4px 0 0;font-size:15px;}");
            sb.AppendLine(".badge{display:inline-block;padding:4px 14px;border-radius:20px;background:#EAFAF1;color:#27AE60;font-weight:bold;font-size:13px;}");
            sb.AppendLine(".info-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px 30px;margin-bottom:20px;}");
            sb.AppendLine(".info-row{display:flex;gap:8px;}");
            sb.AppendLine(".label{color:#555;min-width:120px;}");
            sb.AppendLine(".value{font-weight:600;}");
            sb.AppendLine("table{width:100%;border-collapse:collapse;margin-top:10px;}");
            sb.AppendLine("th{background:#27AE60;color:white;padding:10px;text-align:right;}");
            sb.AppendLine("td{padding:9px 10px;border-bottom:1px solid #eee;}");
            sb.AppendLine("tr:nth-child(even) td{background:#F8F9FA;}");
            sb.AppendLine(".total-row{font-size:18px;font-weight:bold;color:#27AE60;margin-top:16px;text-align:left;}");
            sb.AppendLine(".footer{margin-top:30px;text-align:center;color:#999;font-size:12px;border-top:1px solid #eee;padding-top:12px;}");
            sb.AppendLine("@media print{button{display:none;}}");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<div class=\"header\">");
            sb.AppendLine("<h1> مصنع البلوك</h1>");
            sb.AppendLine("<h2>فاتورة سحب</h2>");
            sb.AppendLine("<span class=\"badge\">" + w.WithdrawalNumber + "</span>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"info-grid\">");
            sb.AppendLine("<div class=\"info-row\"><span class=\"label\">العميل:</span><span class=\"value\">" + (w.Reservation?.Customer?.FullName ?? "—") + "</span></div>");
            sb.AppendLine("<div class=\"info-row\"><span class=\"label\">رقم الحجز:</span><span class=\"value\">" + (w.Reservation?.ReservationNumber ?? "—") + "</span></div>");
            sb.AppendLine("<div class=\"info-row\"><span class=\"label\">تاريخ السحب:</span><span class=\"value\">" + w.WithdrawalDate.ToString("dd/MM/yyyy") + "</span></div>");
            sb.AppendLine(notesRow);
            sb.AppendLine("</div>");
            sb.AppendLine("<table>");
            sb.AppendLine("<thead><tr><th>المنتج</th><th>الكمية</th><th>السعر</th><th>الإجمالي</th></tr></thead>");
            sb.AppendLine("<tbody>" + itemsRows + "</tbody>");
            sb.AppendLine("</table>");
            sb.AppendLine("<div class=\"total-row\"> إجمالي السحب: " + w.TotalAmount.ToString("N0") + " ر.ي</div>");
            sb.AppendLine("<div class=\"footer\">نظام إدارة مصنع البلوك — تم الإصدار: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "</div>");
            sb.AppendLine("<script>window.onload=function(){window.print();}</script>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static WithdrawalListDto MapWithdrawal(WithdrawalTransaction w)
        {
            var (statusText, statusColor) = w.Status == WithdrawalStatus.Completed
                ? ("مكتمل", "#27AE60")
                : ("ملغي", "#E74C3C");

            return new WithdrawalListDto
            {
                Id = w.Id,
                WithdrawalNumber = w.WithdrawalNumber,
                WithdrawalDate = w.WithdrawalDate,
                TotalAmount = w.TotalAmount,
                StatusText = statusText,
                StatusColor = statusColor,
                Notes = w.Notes,
                Items = w.Items.Select(i => new WithdrawalItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalAmount = i.TotalAmount
                }).ToList()
            };
        }
    }
}

/*
using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.Reservations;
using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Inventory;
using BlockFactory.Core.Models.Reservations;
using BlockFactory.Core.Models.Sales;
using BlockFactory.Core.Session;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Core.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IUnitOfWork _uow;

        public ReservationService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ════════════════════════════════════════════
        // إنشاء الحجز
        // ════════════════════════════════════════════

        public async Task<ServiceResult<int>> CreateReservationAsync(
            CreateReservationDto dto)
        {
            // ── التحقق الأساسي ──
            if (dto.AmountPaid <= 0)
                return ServiceResult<int>.Fail("المبلغ المدفوع يجب أن يكون أكبر من صفر");

            var customer = await _uow.Customers.GetByIdAsync(dto.CustomerId);
            if (customer == null)
                return ServiceResult<int>.Fail("العميل غير موجود");

            // ── التحقق الخاص بالحجز المحدد ──
            if (dto.Type == ReservationType.QuantityReservation)
            {
                if (!dto.Items.Any())
                    return ServiceResult<int>.Fail(
                        "الحجز المحدد يتطلب تحديد نوع وكمية البلك");

                // التحقق من توافر الكميات
                foreach (var item in dto.Items)
                {
                    var stock = await _uow.Inventory.GetByProductAsync(item.ProductId);
                    if (stock == null)
                        return ServiceResult<int>.Fail(
                            $"لم يُعثر على مخزون للمنتج ID={item.ProductId}");

                    if (!dto.SkipStockValidation && stock.FreeQuantity < item.Quantity)
                        return ServiceResult<int>.Fail(
                            $"الكمية المطلوبة ({item.Quantity:N0}) " +
                            $"تتجاوز المتاح للحجز ({stock.FreeQuantity:N0}) " +
                            $"للمنتج: {stock.Product?.Name}");
                }
            }

            await _uow.BeginTransactionAsync();
            try
            {
                // ── إنشاء رقم الفاتورة ──
                var reservationNumber = await GenerateReservationNumberAsync();

                // ── إنشاء الحجز ──
                var reservation = new Reservation
                {
                    ReservationNumber = reservationNumber,
                    Type             = dto.Type,
                    Status           = ReservationStatus.Active,
                    CustomerId       = dto.CustomerId,
                    AmountPaid       = dto.AmountPaid,
                    AmountUsed       = 0,
                    PaymentMethod    = dto.PaymentMethod,
                    WalletName       = dto.WalletName,
                    TransactionReference = dto.TransactionReference,
                    ReservationDate  = dto.ReservationDate,
                    Notes            = dto.Notes,
                    CreatedAt        = DateTime.Now,
                    CreatedByUserId  = CurrentSession.Instance.UserId
                };

                await _uow.Reservations.AddAsync(reservation);
                await _uow.SaveChangesAsync(); // نحصل على reservation.Id

                // ── حفظ أصناف الحجز المحدد + تحديث المخزون ──
                if (dto.Type == ReservationType.QuantityReservation)
                {
                    foreach (var item in dto.Items)
                    {
                        var reservationItem = new ReservationItem
                        {
                            ReservationId    = reservation.Id,
                            ProductId        = item.ProductId,
                            QuantityReserved = item.Quantity,
                            QuantityWithdrawn = 0,
                            UnitPrice        = item.UnitPrice,
                            CreatedAt        = DateTime.Now
                        };
                        await _uow.ReservationItems.AddAsync(reservationItem);

                        // تحديث المخزون: Available → Reserved
                        var stock = await _uow.Inventory.GetByProductAsync(item.ProductId);
                        if (stock != null)
                        {
                            stock.ReservedQuantity += item.Quantity;
                            stock.LastUpdated = DateTime.Now;
                            _uow.Inventory.Update(stock);
                        }
                    }
                    await _uow.SaveChangesAsync();
                }

                // ── حفظ Snapshot كامل للكتالوج ──
                await SavePriceSnapshotAsync(reservation.Id);

                await _uow.CommitAsync();

                return ServiceResult<int>.Ok(
                    reservation.Id,
                    $"تم إنشاء فاتورة الحجز {reservationNumber} بنجاح");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult<int>.Fail($"خطأ أثناء إنشاء الحجز: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════
        // السحب
        // ════════════════════════════════════════════

        public async Task<ServiceResult<int>> CreateWithdrawalAsync(
            CreateWithdrawalDto dto)
        {
            var reservation = await _uow.Reservations
                .Query()
                .Include(r => r.Items)
                    .ThenInclude(i => i.Product)
                .Include(r => r.PriceSnapshots)
                .FirstOrDefaultAsync(r => r.Id == dto.ReservationId
                                       && !r.IsDeleted);

            if (reservation == null)
                return ServiceResult<int>.Fail("فاتورة الحجز غير موجودة");

            if (reservation.Status == ReservationStatus.FullyUsed)
                return ServiceResult<int>.Fail("هذا الحجز مستهلك بالكامل");

            if (reservation.Status == ReservationStatus.Cancelled)
                return ServiceResult<int>.Fail("هذا الحجز ملغي");

            if (!dto.Items.Any())
                return ServiceResult<int>.Fail("يجب تحديد صنف واحد على الأقل للسحب");

            await _uow.BeginTransactionAsync();
            try
            {
                decimal totalAmount = 0;
                var withdrawalItems = new List<WithdrawalItem>();

                foreach (var itemDto in dto.Items)
                {
                    // جلب السعر من Snapshot
                    var snapshot = reservation.PriceSnapshots
                        .FirstOrDefault(s => s.ProductId == itemDto.ProductId);

                    if (snapshot == null)
                        return ServiceResult<int>.Fail(
                            $"لا يوجد سعر محدد للمنتج ID={itemDto.ProductId} في هذا الحجز");

                    var product = await _uow.Products.GetByIdAsync(itemDto.ProductId);

                    // ── التحقق حسب نوع الحجز ──
                    if (reservation.Type == ReservationType.QuantityReservation)
                    {
                        var reservedItem = reservation.Items
                            .FirstOrDefault(i => i.ProductId == itemDto.ProductId);

                        if (reservedItem == null)
                            return ServiceResult<int>.Fail(
                                $"المنتج {snapshot.ProductName} غير موجود في قائمة هذا الحجز المحدد");

                        if (itemDto.Quantity > reservedItem.QuantityRemaining)
                            return ServiceResult<int>.Fail(
                                $"الكمية المطلوبة ({itemDto.Quantity:N0}) تتجاوز " +
                                $"المتبقي المحجوز ({reservedItem.QuantityRemaining:N0}) " +
                                $"للمنتج: {snapshot.ProductName}");

                        // تحديث QuantityWithdrawn في ReservationItem
                        reservedItem.QuantityWithdrawn += itemDto.Quantity;
                        _uow.ReservationItems.Update(reservedItem);

                        // تحديث المخزون: ReservedQuantity يتناقص → تحويل Delivered
                        var stock = await _uow.Inventory.GetByProductAsync(itemDto.ProductId);
                        if (stock != null)
                        {
                            stock.ReservedQuantity  = Math.Max(0,
                                stock.ReservedQuantity - itemDto.Quantity);
                            stock.QuantityAvailable = Math.Max(0,
                                stock.QuantityAvailable - itemDto.Quantity);
                            stock.LastUpdated = DateTime.Now;
                            _uow.Inventory.Update(stock);
                        }
                    }
                    else // OpenBalance
                    {
                        // للحجز المفتوح — التحقق يتم على الرصيد الكلي بعد حساب الكل
                        // المخزون لا يتأثر الآن (يتأثر عند التسليم الفعلي خارج النظام)
                    }

                    decimal itemTotal = itemDto.Quantity * snapshot.Price;
                    totalAmount += itemTotal;

                    withdrawalItems.Add(new WithdrawalItem
                    {
                        ProductId   = itemDto.ProductId,
                        ProductName = snapshot.ProductName,
                        Quantity    = itemDto.Quantity,
                        UnitPrice   = snapshot.Price,
                        CreatedAt   = DateTime.Now
                    });
                }

                // التحقق النهائي للحجز المفتوح
                if (reservation.Type == ReservationType.OpenBalance)
                {
                    if (totalAmount > reservation.AmountRemaining)
                        return ServiceResult<int>.Fail(
                            $"إجمالي السحب ({totalAmount:N0} ر.ي) يتجاوز " +
                            $"الرصيد المتبقي ({reservation.AmountRemaining:N0} ر.ي)");
                }

                // ── إنشاء سجل السحب ──
                var withdrawalNumber = await GenerateWithdrawalNumberAsync();

                var withdrawal = new WithdrawalTransaction
                {
                    ReservationId    = reservation.Id,
                    WithdrawalNumber = withdrawalNumber,
                    WithdrawalDate   = dto.WithdrawalDate,
                    Status           = WithdrawalStatus.Completed,
                    TotalAmount      = totalAmount,
                    Notes            = dto.Notes,
                    CreatedAt        = DateTime.Now,
                    CreatedByUserId  = CurrentSession.Instance.UserId
                };

                await _uow.WithdrawalTransactions.AddAsync(withdrawal);
                await _uow.SaveChangesAsync();

                // ربط أصناف السحب بالسجل
                foreach (var item in withdrawalItems)
                {
                    item.WithdrawalTransactionId = withdrawal.Id;
                    await _uow.WithdrawalItems.AddAsync(item);
                }

                // تحديث الحجز
                reservation.AmountUsed += totalAmount;
                reservation.Status = reservation.AmountRemaining <= 0
                    ? ReservationStatus.FullyUsed
                    : ReservationStatus.PartiallyUsed;
                reservation.UpdatedAt = DateTime.Now;
                _uow.Reservations.Update(reservation);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                return ServiceResult<int>.Ok(
                    withdrawal.Id,
                    $"تم تنفيذ السحب {withdrawalNumber} بقيمة {totalAmount:N0} ر.ي");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult<int>.Fail($"خطأ أثناء تنفيذ السحب: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════
        // إلغاء الحجز
        // ════════════════════════════════════════════

        public async Task<ServiceResult<decimal>> CancelReservationAsync(
            int reservationId, string? cancellationNotes = null)
        {
            var reservation = await _uow.Reservations
                .Query()
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == reservationId && !r.IsDeleted);

            if (reservation == null)
                return ServiceResult<decimal>.Fail("فاتورة الحجز غير موجودة");

            if (reservation.Status == ReservationStatus.Cancelled)
                return ServiceResult<decimal>.Fail("هذا الحجز ملغي مسبقاً");

            if (reservation.Status == ReservationStatus.FullyUsed)
                return ServiceResult<decimal>.Fail(
                    "هذا الحجز مستهلك بالكامل — لا يوجد رصيد للإلغاء");

            var refundAmount = reservation.AmountRemaining;

            await _uow.BeginTransactionAsync();
            try
            {
                // للحجز المحدد: تحرير الكميات المحجوزة المتبقية
                if (reservation.Type == ReservationType.QuantityReservation)
                {
                    foreach (var item in reservation.Items)
                    {
                        int qtyToRelease = item.QuantityRemaining;
                        if (qtyToRelease <= 0) continue;

                        var stock = await _uow.Inventory
                            .GetByProductAsync(item.ProductId);
                        if (stock != null)
                        {
                            stock.ReservedQuantity = Math.Max(0,
                                stock.ReservedQuantity - qtyToRelease);
                            stock.LastUpdated = DateTime.Now;
                            _uow.Inventory.Update(stock);
                        }
                    }
                }

                reservation.Status           = ReservationStatus.Cancelled;
                reservation.CancellationDate = DateTime.Now;
                reservation.RefundedAmount   = refundAmount;
                reservation.Notes = string.IsNullOrEmpty(cancellationNotes)
                    ? reservation.Notes
                    : $"{reservation.Notes}\n[إلغاء]: {cancellationNotes}";
                reservation.UpdatedAt = DateTime.Now;

                _uow.Reservations.Update(reservation);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                return ServiceResult<decimal>.Ok(
                    refundAmount,
                    $"تم إلغاء الحجز. المبلغ المسترد: {refundAmount:N0} ر.ي");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ServiceResult<decimal>.Fail($"خطأ أثناء الإلغاء: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════
        // الاستعلام
        // ════════════════════════════════════════════

        public async Task<ReservationDetailDto?> GetReservationByIdAsync(
            int reservationId)
        {
            var r = await _uow.Reservations
                .Query()
                .Include(r => r.Customer)
                .Include(r => r.Items).ThenInclude(i => i.Product)
                .Include(r => r.PriceSnapshots).ThenInclude(s => s.Product)
                .Include(r => r.Withdrawals).ThenInclude(w => w.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(r => r.Id == reservationId && !r.IsDeleted);

            return r == null ? null : MapToDetail(r);
        }

        public async Task<IEnumerable<ReservationListDto>> GetAllActiveAsync()
        {
            var list = await _uow.Reservations
                .Query()
                .Include(r => r.Customer)
                .Include(r => r.Withdrawals)
                .Where(r => r.Status == ReservationStatus.Active
                         || r.Status == ReservationStatus.PartiallyUsed)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            return list.Select(MapToList);
        }

        public async Task<IEnumerable<ReservationListDto>> GetByCustomerAsync(
            int customerId)
        {
            var list = await _uow.Reservations
                .Query()
                .Include(r => r.Customer)
                .Include(r => r.Withdrawals)
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            return list.Select(MapToList);
        }

        public async Task<CustomerReservationSummaryDto> GetCustomerSummaryAsync(
            int customerId)
        {
            var active = await _uow.Reservations
                .Query()
                .Include(r => r.Customer)
                .Include(r => r.Items)
                .Include(r => r.Withdrawals)
                .Where(r => r.CustomerId == customerId
                         && (r.Status == ReservationStatus.Active
                          || r.Status == ReservationStatus.PartiallyUsed))
                .ToListAsync();

            return new CustomerReservationSummaryDto
            {
                ActiveReservationsCount = active.Count,
                TotalRemainingBalance   = active.Sum(r => r.AmountRemaining),
                TotalQuantityRemaining  = active
                    .Where(r => r.Type == ReservationType.QuantityReservation)
                    .SelectMany(r => r.Items)
                    .Sum(i => i.QuantityRemaining),
                ActiveReservations = active.Select(MapToList).ToList()
            };
        }

        public async Task<IEnumerable<ReservationListDto>> SearchAsync(string keyword)
        {
            var list = await _uow.Reservations
                .Query()
                .Include(r => r.Customer)
                .Include(r => r.Withdrawals)
                .Where(r => r.ReservationNumber.Contains(keyword)
                         || r.Customer.FullName.Contains(keyword))
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            return list.Select(MapToList);
        }

        public async Task<WithdrawalListDto?> GetWithdrawalByIdAsync(int withdrawalId)
        {
            var w = await _uow.WithdrawalTransactions
                .Query()
                .Include(w => w.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(w => w.Id == withdrawalId);

            return w == null ? null : MapWithdrawal(w);
        }

        public async Task<IEnumerable<WithdrawalListDto>> GetWithdrawalsByReservationAsync(
            int reservationId)
        {
            var list = await _uow.WithdrawalTransactions
                .Query()
                .Include(w => w.Items).ThenInclude(i => i.Product)
                .Where(w => w.ReservationId == reservationId)
                .OrderByDescending(w => w.WithdrawalDate)
                .ToListAsync();

            return list.Select(MapWithdrawal);
        }

        public async Task<IEnumerable<PriceSnapshotDto>> GetPriceSnapshotAsync(
            int reservationId)
        {
            var snapshots = await _uow.PriceSnapshots
                .Query()
                .Include(s => s.Product)
                .Where(s => s.ReservationId == reservationId)
                .OrderBy(s => s.ProductName)
                .ToListAsync();

            return snapshots.Select(s => new PriceSnapshotDto
            {
                ProductId   = s.ProductId,
                ProductName = s.ProductName,
                Price       = s.Price,
                PriceMin    = s.PriceMin,
                PriceMax    = s.PriceMax
            });
        }

        // ════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════

        private async Task SavePriceSnapshotAsync(int reservationId)
        {
            var products = await _uow.Products
                .Query()
                .Include(p => p.ProductType)
                .Where(p => p.IsActive && !p.IsDeleted)
                .ToListAsync();

            foreach (var product in products)
            {
                var snapshot = new PriceSnapshot
                {
                    ReservationId = reservationId,
                    ProductId     = product.Id,
                    ProductName   = product.Name,
                    Price         = product.DefaultPrice,
                    PriceMin      = product.PriceMin,
                    PriceMax      = product.PriceMax,
                    SnapshotDate  = DateTime.Now,
                    CreatedAt     = DateTime.Now
                };
                await _uow.PriceSnapshots.AddAsync(snapshot);
            }
            await _uow.SaveChangesAsync();
        }

        private async Task<string> GenerateReservationNumberAsync()
        {
            var today  = DateTime.Today;
            var prefix = $"RES-{today:yyyyMMdd}-";
            var count  = await _uow.Reservations
                .Query()
                .CountAsync(r => r.ReservationNumber.StartsWith(prefix));
            return $"{prefix}{(count + 1):D3}";
        }

        private async Task<string> GenerateWithdrawalNumberAsync()
        {
            var today  = DateTime.Today;
            var prefix = $"WD-{today:yyyyMMdd}-";
            var count  = await _uow.WithdrawalTransactions
                .Query()
                .CountAsync(w => w.WithdrawalNumber.StartsWith(prefix));
            return $"{prefix}{(count + 1):D3}";
        }

        // ─── Mappers ────────────────────────────────

        private static ReservationListDto MapToList(Reservation r)
        {
            var (typeText, typeColor) = r.Type == ReservationType.QuantityReservation
                ? ("حجز محدد", "#2980B9")
                : ("حجز مفتوح", "#8E44AD");

            var (statusText, statusColor) = r.Status switch
            {
                ReservationStatus.Active        => ("نشط", "#27AE60"),
                ReservationStatus.PartiallyUsed => ("مستهلك جزئياً", "#F39C12"),
                ReservationStatus.FullyUsed     => ("مستهلك كلياً", "#7F8C8D"),
                ReservationStatus.Cancelled     => ("ملغي", "#E74C3C"),
                _                               => ("غير معروف", "#95A5A6")
            };

            return new ReservationListDto
            {
                Id                = r.Id,
                ReservationNumber = r.ReservationNumber,
                CustomerName      = r.Customer?.FullName ?? string.Empty,
                CustomerPhone     = r.Customer?.Phone,
                TypeText          = typeText,
                TypeColor         = typeColor,
                StatusText        = statusText,
                StatusColor       = statusColor,
                AmountPaid        = r.AmountPaid,
                AmountUsed        = r.AmountUsed,
                AmountRemaining   = r.AmountRemaining,
                ReservationDate   = r.ReservationDate,
                WithdrawalsCount  = r.Withdrawals?.Count ?? 0
            };
        }

        private static ReservationDetailDto MapToDetail(Reservation r)
        {
            var list = MapToList(r);
            return new ReservationDetailDto
            {
                Id                  = r.Id,
                ReservationNumber   = r.ReservationNumber,
                CustomerId          = r.CustomerId,
                CustomerName        = list.CustomerName,
                CustomerPhone       = list.CustomerPhone,
                Type                = r.Type,
                TypeText            = list.TypeText,
                Status              = r.Status,
                StatusText          = list.StatusText,
                StatusColor         = list.StatusColor,
                AmountPaid          = r.AmountPaid,
                AmountUsed          = r.AmountUsed,
                AmountRemaining     = r.AmountRemaining,
                PaymentMethodText   = r.PaymentMethod.ToString(),
                WalletName          = r.WalletName,
                TransactionReference = r.TransactionReference,
                ReservationDate     = r.ReservationDate,
                Notes               = r.Notes,
                Items               = r.Items.Select(i => new ReservationItemDto
                {
                    Id                = i.Id,
                    ProductId         = i.ProductId,
                    ProductName       = i.Product?.Name ?? string.Empty,
                    QuantityReserved  = i.QuantityReserved,
                    QuantityWithdrawn = i.QuantityWithdrawn,
                    QuantityRemaining = i.QuantityRemaining,
                    UnitPrice         = i.UnitPrice,
                    TotalAmount       = i.TotalAmount,
                    StatusColor       = i.QuantityRemaining == 0 ? "#7F8C8D" : "#27AE60"
                }).ToList(),
                PriceSnapshots = r.PriceSnapshots.Select(s => new PriceSnapshotDto
                {
                    ProductId   = s.ProductId,
                    ProductName = s.ProductName,
                    Price       = s.Price,
                    PriceMin    = s.PriceMin,
                    PriceMax    = s.PriceMax
                }).ToList(),
                Withdrawals = r.Withdrawals.Select(MapWithdrawal).ToList()
            };
        }

        private static WithdrawalListDto MapWithdrawal(WithdrawalTransaction w)
        {
            var (statusText, statusColor) = w.Status == WithdrawalStatus.Completed
                ? ("مكتمل", "#27AE60")
                : ("ملغي", "#E74C3C");

            return new WithdrawalListDto
            {
                Id               = w.Id,
                WithdrawalNumber = w.WithdrawalNumber,
                WithdrawalDate   = w.WithdrawalDate,
                TotalAmount      = w.TotalAmount,
                StatusText       = statusText,
                StatusColor      = statusColor,
                Notes            = w.Notes,
                Items            = w.Items.Select(i => new WithdrawalItemDto
                {
                    ProductId   = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity    = i.Quantity,
                    UnitPrice   = i.UnitPrice,
                    TotalAmount = i.TotalAmount
                }).ToList()
            };
        }
    }
}*/

