// BlockFactory.Core/Models/Reservations/WithdrawalTransaction.cs

using BlockFactory.Core.Models.Base;

namespace BlockFactory.Core.Models.Reservations
{
    /// <summary>
    /// سجل السحب — عملية استهلاك من رصيد الحجز
    ///
    /// للحجز المحدد:  الخصم من الكميات المحجوزة → Reserved→Delivered
    /// للحجز المفتوح: الخصم من الرصيد المالي → AmountUsed += TotalAmount
    ///
    /// ليس Order (ليس بيعاً جديداً) — هو تنفيذ من رصيد مدفوع مسبقاً
    /// </summary>
    public class WithdrawalTransaction : BaseEntity
    {
        // ─── FK للحجز ──────────────────────────────
        public int ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;

        // ─── رقم السحب ─────────────────────────────
        /// <summary>رقم السحب (WD-20250515-001)</summary>
        public string WithdrawalNumber { get; set; } = string.Empty;

        // ─── التاريخ والحالة ───────────────────────
        public DateTime WithdrawalDate { get; set; } = DateTime.Now;
        public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Completed;

        // ─── المبلغ الإجمالي ───────────────────────
        /// <summary>
        /// إجمالي قيمة هذا السحب بالسعر المثبت
        /// = مجموع (Quantity × UnitPrice من PriceSnapshot) لكل صنف
        /// </summary>
        public decimal TotalAmount { get; set; }

        // ─── ملاحظات ───────────────────────────────
        public string? Notes { get; set; }

        // ─── Navigation ────────────────────────────
        public ICollection<WithdrawalItem> Items { get; set; } = new List<WithdrawalItem>();
    }
}
