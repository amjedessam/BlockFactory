// BlockFactory.Core/Interfaces/Services/IReservationService.cs

using BlockFactory.Core.Common;
using BlockFactory.Core.DTOs.Reservations;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface IReservationService
    {
        // ─── إنشاء الحجوزات ────────────────────────

        /// <summary>
        /// إنشاء فاتورة حجز جديدة (محدد أو مفتوح)
        /// يحفظ Snapshot كامل للكتالوج
        /// في الحجز المحدد: يزيد ReservedQuantity في المخزون
        /// </summary>
        Task<ServiceResult<int>> CreateReservationAsync(CreateReservationDto dto);

        // ─── الاستعلام ─────────────────────────────

        Task<ReservationDetailDto?> GetReservationByIdAsync(int reservationId);

        Task<IEnumerable<ReservationListDto>> GetAllActiveAsync();

        Task<IEnumerable<ReservationListDto>> GetByCustomerAsync(int customerId);

        Task<CustomerReservationSummaryDto> GetCustomerSummaryAsync(int customerId);

        /// <summary>البحث برقم الفاتورة أو اسم العميل</summary>
        Task<IEnumerable<ReservationListDto>> SearchAsync(string keyword);

        // ─── السحب ─────────────────────────────────

        /// <summary>
        /// تنفيذ سحب من حجز موجود
        ///
        /// للحجز المحدد:
        ///   - يتحقق أن الكمية المطلوبة ≤ الكمية المتبقية لكل صنف
        ///   - يزيد QuantityWithdrawn في ReservationItems
        ///   - يخصم ReservedQuantity من InventoryStock
        ///
        /// للحجز المفتوح:
        ///   - يتحقق أن المبلغ الإجمالي ≤ AmountRemaining
        ///   - يحضر السعر من PriceSnapshot تلقائياً
        ///   - لا يغير ReservedQuantity (المخزون يتأثر عند التسليم الفعلي)
        ///
        /// في كلتا الحالتين:
        ///   - يزيد Reservation.AmountUsed
        ///   - يحدّث Reservation.Status تلقائياً
        /// </summary>
        Task<ServiceResult<int>> CreateWithdrawalAsync(CreateWithdrawalDto dto);

        Task<WithdrawalListDto?> GetWithdrawalByIdAsync(int withdrawalId);

        Task<IEnumerable<WithdrawalListDto>> GetWithdrawalsByReservationAsync(int reservationId);

        // ─── إلغاء الحجز ───────────────────────────

        /// <summary>
        /// إلغاء الحجز
        /// يرجع فقط AmountRemaining (المتبقي غير المستهلك)
        /// لا يرجع المستهلك سابقاً
        ///
        /// في الحجز المحدد:
        ///   - يُفرج عن ReservedQuantity المتبقية في InventoryStock
        ///
        /// يحدّث Status إلى Cancelled
        /// يحفظ RefundedAmount
        /// </summary>
        Task<ServiceResult<decimal>> CancelReservationAsync(
            int reservationId, string? cancellationNotes = null);

        // ─── مساعدات الواجهة ───────────────────────

        /// <summary>
        /// جلب أسعار الكتالوج المثبتة لحجز معين
        /// تُستخدم في شاشة السحب لعرض الأسعار المتاحة
        /// </summary>
        Task<IEnumerable<PriceSnapshotDto>> GetPriceSnapshotAsync(int reservationId);
    }
}
