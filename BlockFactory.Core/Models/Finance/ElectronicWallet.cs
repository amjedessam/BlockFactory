using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Finance
{
    public class ElectronicWallet : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        // كاش، سبأفون، وان كاش...

        public string? AccountNumber { get; set; }
        public decimal Balance { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<WalletTransaction> Transactions { get; set; }
            = new List<WalletTransaction>();
    }
}
