using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Auth
{
    public class Role : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        // Admin, Accountant

        public ICollection<User> Users { get; set; }
            = new List<User>();
    }
}
