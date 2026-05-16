using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Models.HR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockFactory.Data.Configurations
{
    public class WorkerConfiguration : IEntityTypeConfiguration<Worker>
    {
        public void Configure(EntityTypeBuilder<Worker> builder)
        {
            builder.ToTable("Workers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.BasicSalary)
                .HasColumnType("decimal(18,2)");

            // السلف
            builder.HasMany(x => x.Advances)
                .WithOne(x => x.Worker)
                .HasForeignKey(x => x.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);

            // الخصومات
            builder.HasMany(x => x.Deductions)
                .WithOne(x => x.Worker)
                .HasForeignKey(x => x.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);

            // الرواتب
            builder.HasMany(x => x.Salaries)
                .WithOne(x => x.Worker)
                .HasForeignKey(x => x.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
