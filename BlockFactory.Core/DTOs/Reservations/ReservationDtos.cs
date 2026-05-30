// BlockFactory.Core/DTOs/Reservations/ReservationDtos.cs

using BlockFactory.Core.Models.Reservations;
using BlockFactory.Core.Models.Sales;

namespace BlockFactory.Core.DTOs.Reservations
{
    // ════════════════════════════════════════════════
    // DTOs الإنشاء
    // ════════════════════════════════════════════════

    /// <summary>DTO إنشاء فاتورة حجز جديدة</summary>
    public class CreateReservationDto
    {
        public int CustomerId { get; set; }
        public ReservationType Type { get; set; }
        public decimal AmountPaid { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? WalletName { get; set; }
        public string? TransactionReference { get; set; }
        public DateTime ReservationDate { get; set; } = DateTime.Today;
        public string? Notes { get; set; }

        /// <summary>
        /// أصناف الحجز المحدد — فارغة في OpenBalance
        /// إذا Type = QuantityReservation، يجب أن تحتوي على صنف واحد على الأقل
        /// </summary>
        public List<CreateReservationItemDto> Items { get; set; } = new();
        /// <summary>
        /// عند true يتجاهل خدمة الحجز التحقق من الكمية المتاحة
        /// ويجوز إنشاء الحجز حتى لو كانت الكمية المطلوبة أكبر من المتوفر.
        /// تُستخدم عند موافقة المستخدم الصريحة على التجاوز.
        /// </summary>
        public bool SkipStockValidation { get; set; } = false;
    }

    /// <summary>صنف الحجز المحدد</summary>
    public class CreateReservationItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        /// <summary>السعر الذي وافق عليه العميل (داخل نطاق PriceMin-PriceMax)</summary>
        public decimal UnitPrice { get; set; }
    }

    /// <summary>DTO إنشاء سحب من الحجز</summary>
    public class CreateWithdrawalDto
    {
        public int ReservationId { get; set; }
        public DateTime WithdrawalDate { get; set; } = DateTime.Today;
        public string? Notes { get; set; }
        public List<CreateWithdrawalItemDto> Items { get; set; } = new();
    }

    /// <summary>صنف في طلب السحب</summary>
    public class CreateWithdrawalItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        // السعر يُحضر تلقائياً من PriceSnapshot — لا يُدخل يدوياً
    }

    // ════════════════════════════════════════════════
    // DTOs العرض
    // ════════════════════════════════════════════════

    /// <summary>قائمة الحجوزات</summary>
    public class ReservationListDto
    {
        public int Id { get; set; }
        public string ReservationNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string TypeText { get; set; } = string.Empty;
        public string TypeColor { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal AmountUsed { get; set; }
        public decimal AmountRemaining { get; set; }
        public DateTime ReservationDate { get; set; }
        public int WithdrawalsCount { get; set; }
    }

    /// <summary>تفاصيل الحجز الكاملة</summary>
    public class ReservationDetailDto
    {
        public int Id { get; set; }
        public string ReservationNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public ReservationType Type { get; set; }
        public string TypeText { get; set; } = string.Empty;
        public ReservationStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal AmountUsed { get; set; }
        public decimal AmountRemaining { get; set; }
        public string PaymentMethodText { get; set; } = string.Empty;
        public string? WalletName { get; set; }
        public string? TransactionReference { get; set; }
        public DateTime ReservationDate { get; set; }
        public string? Notes { get; set; }

        /// <summary>أصناف الحجز المحدد — فارغة في OpenBalance</summary>
        public List<ReservationItemDto> Items { get; set; } = new();

        /// <summary>أسعار الكتالوج المثبتة وقت الحجز</summary>
        public List<PriceSnapshotDto> PriceSnapshots { get; set; } = new();

        /// <summary>سجلات السحب</summary>
        public List<WithdrawalListDto> Withdrawals { get; set; } = new();
    }

    /// <summary>صنف الحجز المحدد</summary>
    public class ReservationItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantityReserved { get; set; }
        public int QuantityWithdrawn { get; set; }
        public int QuantityRemaining { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public string StatusColor { get; set; } = string.Empty;
    }

    /// <summary>سعر من الـ Snapshot</summary>
    public class PriceSnapshotDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
    }

    /// <summary>قائمة السحوبات</summary>
    public class WithdrawalListDto
    {
        public int Id { get; set; }
        public string WithdrawalNumber { get; set; } = string.Empty;
        public DateTime WithdrawalDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<WithdrawalItemDto> Items { get; set; } = new();
    }

    /// <summary>صنف السحب</summary>
    public class WithdrawalItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
    }

    /// <summary>ملخص حجوزات العميل للعرض في شاشة العميل</summary>
    public class CustomerReservationSummaryDto
    {
        public int ActiveReservationsCount { get; set; }
        public decimal TotalRemainingBalance { get; set; }
        public decimal TotalQuantityRemaining { get; set; }
        public List<ReservationListDto> ActiveReservations { get; set; } = new();
    }
}
