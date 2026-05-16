using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Inventory;
using BlockFactory.Core.Models.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockFactory.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.PriceMin)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.PriceMax)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.DefaultPrice)
                .HasColumnType("decimal(18,2)");

            // علاقة مع ProductType
            builder.HasOne(x => x.ProductType)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.ProductTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // علاقة واحد لواحد مع المخزون
            builder.HasOne(x => x.Stock)
                .WithOne(x => x.Product)
                .HasForeignKey<InventoryStock>(x => x.ProductId);
        }
    }
}
