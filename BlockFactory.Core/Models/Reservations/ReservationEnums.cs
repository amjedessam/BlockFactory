// BlockFactory.Core/Models/Reservations/ReservationEnums.cs

namespace BlockFactory.Core.Models.Reservations
{
    /// <summary>
    /// نوع الحجز
    /// </summary>
    public enum ReservationType
    {
        /// <summary>
        /// حجز محدد — العميل يحدد النوع والكمية من البداية
        /// الخصم يكون من الكميات المحجوزة
        /// </summary>
        QuantityReservation = 1,

        /// <summary>
        /// حجز مفتوح — العميل يدفع مبلغاً فقط بدون تحديد
        /// الخصم يكون من الرصيد المالي
        /// </summary>
        OpenBalance = 2
    }

    /// <summary>
    /// حالة الحجز
    /// </summary>
    public enum ReservationStatus
    {
        /// <summary>نشط — لم يُستهلك منه شيء</summary>
        Active = 1,

        /// <summary>مستهلك جزئياً</summary>
        PartiallyUsed = 2,

        /// <summary>مستهلك بالكامل</summary>
        FullyUsed = 3,

        /// <summary>ملغي — تم إرجاع الرصيد المتبقي للعميل</summary>
        Cancelled = 4
    }

    /// <summary>
    /// حالة سجل السحب
    /// </summary>
    public enum WithdrawalStatus
    {
        /// <summary>مكتمل</summary>
        Completed = 1,

        /// <summary>ملغي</summary>
        Cancelled = 2
    }
}
