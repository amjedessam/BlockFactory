using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Base;
namespace BlockFactory.Core.Models.Auth
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }

        // FK
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
}
