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
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(x => x.Code)
                .IsUnique();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Balance)
                .HasColumnType("decimal(18,2)");

            // علاقة الحساب الأب والفروع
            builder.HasOne(x => x.ParentAccount)
                .WithMany(x => x.SubAccounts)
                .HasForeignKey(x => x.ParentAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
