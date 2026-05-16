using BlockFactory.Core.Models.Auth;
using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Finance
{
    public class ActivityLog : BaseEntity
    {
        public string Action { get; set; } = string.Empty;
        // Created, Updated, Deleted, Printed...

        public string EntityName { get; set; } = string.Empty;
        // Order, Invoice, Worker...

        public int? EntityId { get; set; }
        public string? OldValues { get; set; }
        // JSON snapshot قبل التعديل

        public string? NewValues { get; set; }
        // JSON snapshot بعد التعديل

        public string? IpAddress { get; set; }
        public DateTime LoggedAt { get; set; } = DateTime.Now;

        // FK
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
