using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Customers;
using BlockFactory.Core.Models.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockFactory.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.OrderNumber)
                .IsRequired()
                .HasMaxLength(20);

            // Index فريد على رقم الطلب
            builder.HasIndex(x => x.OrderNumber)
                .IsUnique();

            builder.Property(x => x.SubTotal)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.Discount)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.TotalAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.PaidAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.RemainingAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.DeliveryCost)
                .HasColumnType("decimal(18,2)");

            // علاقة مع العميل
            builder.HasOne(x => x.Customer)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // علاقة مع الفاتورة
            builder.HasOne(x => x.Invoice)
                .WithOne(x => x.Order)
                .HasForeignKey<Invoice>(x => x.OrderId);

            // علاقة مع الرهن
            builder.HasOne(x => x.Pledge)
                .WithOne(x => x.Order)
                .HasForeignKey<Pledge>(x => x.OrderId);
        }
    }
}
