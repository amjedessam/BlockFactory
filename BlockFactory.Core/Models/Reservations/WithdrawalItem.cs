// BlockFactory.Core/Models/Reservations/WithdrawalItem.cs

using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Products;

namespace BlockFactory.Core.Models.Reservations
{
    /// <summary>
    /// صنف داخل سجل السحب
    /// السعر يأتي دائماً من PriceSnapshot الخاص بالحجز
    /// </summary>
    public class WithdrawalItem : BaseEntity
    {
        // ─── FK للسحب ──────────────────────────────
        public int WithdrawalTransactionId { get; set; }
        public WithdrawalTransaction WithdrawalTransaction { get; set; } = null!;

        // ─── المنتج ────────────────────────────────
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        /// <summary>اسم المنتج محفوظ للتاريخ</summary>
        public string ProductName { get; set; } = string.Empty;

        // ─── الكمية والسعر ─────────────────────────
        public int Quantity { get; set; }

        /// <summary>
        /// السعر المثبت — مأخوذ من PriceSnapshot الخاص بالحجز
        /// لا يُكتب يدوياً — يُحضر من الـ Snapshot تلقائياً
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>الإجمالي = Quantity × UnitPrice</summary>
        public decimal TotalAmount => Quantity * UnitPrice;
    }
}
