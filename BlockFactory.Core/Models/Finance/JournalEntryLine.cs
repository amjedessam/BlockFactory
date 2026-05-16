using BlockFactory.Core.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Models.Finance
{
    public class JournalEntryLine : BaseEntity
    {
        public decimal DebitAmount { get; set; } = 0;
        public decimal CreditAmount { get; set; } = 0;
        public string? Description { get; set; }

        // FK
        public int JournalEntryId { get; set; }
        public JournalEntry JournalEntry { get; set; } = null!;

        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;
    }
}
