using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Finance
{
    public enum WalletTransactionType
    {
        In,     // استلام
        Out     // دفع
    }

    public class WalletTransaction : BaseEntity
    {
        public WalletTransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string? Reference { get; set; }
        public string? Notes { get; set; }

        // FK
        public int WalletId { get; set; }
        public ElectronicWallet Wallet { get; set; } = null!;
    }
}
