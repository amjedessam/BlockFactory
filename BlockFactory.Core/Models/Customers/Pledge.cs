using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Sales;

namespace BlockFactory.Core.Models.Customers
{
    public enum PledgeType
    {
        Gold,       // ذهب
        Weapon,     // سلاح
        LandDeed,   // ورقة أرض
        Other       // أخرى
    }

    public enum PledgeStatus
    {
        Active,     // الرهن موجود في المصنع
        Returned,   // تم إرجاع الرهن بعد السداد
        Forfeited   // تم الاستيلاء (حالة خاصة)
    }

    public class Pledge : BaseEntity
    {
        public string Description { get; set; } = string.Empty;
        public PledgeType PledgeType { get; set; }
        public string? PledgeTypeOther { get; set; }
        public PledgeStatus Status { get; set; } = PledgeStatus.Active;
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string? Notes { get; set; }

        // FK
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public int? OrderId { get; set; }
        public Order? Order { get; set; }
    }
}
