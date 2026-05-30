// BlockFactory.Core/Models/Reservations/ReservationItem.cs

using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Products;

namespace BlockFactory.Core.Models.Reservations
{
    /// <summary>
    /// صنف في فاتورة الحجز المحدد
    /// موجود فقط في ReservationType.QuantityReservation
    /// </summary>
    public class ReservationItem : BaseEntity
    {
        // ─── FK للحجز ──────────────────────────────
        public int ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;

        // ─── المنتج ────────────────────────────────
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // ─── الكميات ───────────────────────────────
        /// <summary>الكمية المحجوزة الأصلية</summary>
        public int QuantityReserved { get; set; }

        /// <summary>الكمية المسحوبة حتى الآن</summary>
        public int QuantityWithdrawn { get; set; } = 0;

        /// <summary>الكمية المتبقية = Reserved - Withdrawn</summary>
        public int QuantityRemaining => QuantityReserved - QuantityWithdrawn;

        // ─── السعر المثبت ──────────────────────────
        /// <summary>
        /// سعر الوحدة المثبت وقت الحجز — لا يتغير أبداً
        /// حتى لو تغير السعر في النظام لاحقاً
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>إجمالي الصنف = QuantityReserved × UnitPrice</summary>
        public decimal TotalAmount => QuantityReserved * UnitPrice;
    }
}
