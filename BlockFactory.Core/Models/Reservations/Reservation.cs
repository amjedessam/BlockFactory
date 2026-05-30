// BlockFactory.Core/Models/Reservations/Reservation.cs

using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Customers;
using BlockFactory.Core.Models.Sales;

namespace BlockFactory.Core.Models.Reservations
{
    /// <summary>
    /// فاتورة الحجز — تمثل دفعة مسبقة من العميل
    /// نوعان: حجز محدد (كميات) أو حجز مفتوح (رصيد مالي)
    /// </summary>
    public class Reservation : BaseEntity
    {
        // ─── معرّف الفاتورة ────────────────────────
        /// <summary>رقم الفاتورة — يُولّد تلقائياً (RES-20250515-001)</summary>
        public string ReservationNumber { get; set; } = string.Empty;

        // ─── نوع وحالة الحجز ──────────────────────
        public ReservationType Type { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Active;

        // ─── بيانات العميل ─────────────────────────
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        // ─── المبالغ ───────────────────────────────
        /// <summary>المبلغ المدفوع من العميل</summary>
        public decimal AmountPaid { get; set; }

        /// <summary>
        /// المبلغ المستهلك حتى الآن
        /// للحجز المحدد: مجموع قيم السحوبات المكتملة
        /// للحجز المفتوح: مجموع مبالغ السحوبات
        /// </summary>
        public decimal AmountUsed { get; set; } = 0;

        /// <summary>المبلغ المتبقي = AmountPaid - AmountUsed</summary>
        public decimal AmountRemaining => AmountPaid - AmountUsed;

        // ─── بيانات الدفع ──────────────────────────
        public PaymentMethod PaymentMethod { get; set; }
        public string? WalletName { get; set; }
        public string? TransactionReference { get; set; }

        // ─── التواريخ ──────────────────────────────
        public DateTime ReservationDate { get; set; } = DateTime.Now;

        /// <summary>تاريخ الإلغاء (إن ألغي)</summary>
        public DateTime? CancellationDate { get; set; }

        /// <summary>مبلغ المسترد عند الإلغاء</summary>
        public decimal? RefundedAmount { get; set; }

        // ─── ملاحظات ───────────────────────────────
        public string? Notes { get; set; }

        // ─── Navigation Properties ─────────────────

        /// <summary>
        /// أصناف الحجز — للحجز المحدد فقط
        /// فارغة في حالة OpenBalance
        /// </summary>
        public ICollection<ReservationItem> Items { get; set; } = new List<ReservationItem>();

        /// <summary>
        /// Snapshot كامل لأسعار جميع المنتجات وقت الحجز
        /// موجود في كلا النوعين
        /// </summary>
        public ICollection<PriceSnapshot> PriceSnapshots { get; set; } = new List<PriceSnapshot>();

        /// <summary>سجلات السحب من هذا الحجز</summary>
        public ICollection<WithdrawalTransaction> Withdrawals { get; set; } = new List<WithdrawalTransaction>();
    }
}
