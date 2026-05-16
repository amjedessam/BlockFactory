using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Models.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockFactory.Data.Configurations
{
    public class JournalEntryConfiguration
        : IEntityTypeConfiguration<JournalEntry>
    {
        public void Configure(EntityTypeBuilder<JournalEntry> builder)
        {
            builder.ToTable("JournalEntries");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntryNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(x => x.EntryNumber)
                .IsUnique();

            builder.Property(x => x.TotalDebit)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.TotalCredit)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.JournalEntry)
                .HasForeignKey(x => x.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
