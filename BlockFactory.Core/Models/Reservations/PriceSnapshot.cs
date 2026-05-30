// BlockFactory.Core/Models/Reservations/PriceSnapshot.cs

using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Products;

namespace BlockFactory.Core.Models.Reservations
{
    /// <summary>
    /// Snapshot كامل لأسعار الكتالوج وقت إنشاء الحجز
    /// يُحفظ لجميع المنتجات النشطة — في كلا نوعي الحجز
    ///
    /// الهدف: تثبيت الأسعار — العميل يستخدم سعر يوم الحجز دائماً
    /// حتى لو تغيرت أسعار المصنع لاحقاً
    /// </summary>
    public class PriceSnapshot : BaseEntity
    {
        // ─── FK للحجز ──────────────────────────────
        public int ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;

        // ─── المنتج ────────────────────────────────
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        /// <summary>اسم المنتج — محفوظ للتاريخ حتى لو حُذف المنتج</summary>
        public string ProductName { get; set; } = string.Empty;

        // ─── السعر المثبت ──────────────────────────
        /// <summary>السعر الافتراضي للمنتج وقت الحجز</summary>
        public decimal Price { get; set; }

        /// <summary>الحد الأدنى للسعر وقت الحجز</summary>
        public decimal PriceMin { get; set; }

        /// <summary>الحد الأعلى للسعر وقت الحجز</summary>
        public decimal PriceMax { get; set; }

        /// <summary>تاريخ أخذ الـ Snapshot</summary>
        public DateTime SnapshotDate { get; set; } = DateTime.Now;
    }
}
