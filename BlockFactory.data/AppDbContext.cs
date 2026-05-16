using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Models.Auth;
using BlockFactory.Core.Models.Base;
using BlockFactory.Core.Models.Customers;
using BlockFactory.Core.Models.Finance;
using BlockFactory.Core.Models.HR;
using BlockFactory.Core.Models.Inventory;
using BlockFactory.Core.Models.Production;
using BlockFactory.Core.Models.Products;
using BlockFactory.Core.Models.Sales;
using BlockFactory.Core.Models.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // ─── AUTH ───────────────────────────────────
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();

        // ─── PRODUCTS ───────────────────────────────
        public DbSet<ProductType> ProductTypes => Set<ProductType>();
        public DbSet<Product> Products => Set<Product>();

        // ─── CUSTOMERS ──────────────────────────────
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Pledge> Pledges => Set<Pledge>();

        // ─── SALES ──────────────────────────────────
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Invoice> Invoices => Set<Invoice>();

        // ─── PRODUCTION ─────────────────────────────
        public DbSet<ProductionRecord> ProductionRecords => Set<ProductionRecord>();
        public DbSet<ProductionFormula> ProductionFormulas => Set<ProductionFormula>();
        public DbSet<ProductionMaterialUsage> ProductionMaterialUsages
            => Set<ProductionMaterialUsage>();

        // ─── INVENTORY ──────────────────────────────
        public DbSet<InventoryStock> InventoryStocks => Set<InventoryStock>();
        public DbSet<InventoryTransaction> InventoryTransactions
            => Set<InventoryTransaction>();
        public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();
        public DbSet<RawMaterialTransaction> RawMaterialTransactions
            => Set<RawMaterialTransaction>();

        // ─── SUPPLIERS ──────────────────────────────
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<SupplierInvoice> SupplierInvoices => Set<SupplierInvoice>();
        public DbSet<SupplierInvoiceItem> SupplierInvoiceItems
            => Set<SupplierInvoiceItem>();
        public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();

        // ─── HR ─────────────────────────────────────
        public DbSet<Worker> Workers => Set<Worker>();
        public DbSet<MonthlySalary> MonthlySalaries => Set<MonthlySalary>();
        public DbSet<Advance> Advances => Set<Advance>();
        public DbSet<Deduction> Deductions => Set<Deduction>();

        // ─── FINANCE ────────────────────────────────
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
        public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<ElectronicWallet> ElectronicWallets => Set<ElectronicWallet>();
        public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // تطبيق كل الـ Configurations تلقائياً
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly
            );

            // Global Query Filter — إخفاء المحذوفات تلقائياً
            modelBuilder.Entity<User>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Customer>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Order>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Product>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Worker>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Supplier>()
                .HasQueryFilter(x => !x.IsDeleted);
        }

        // Auto-set UpdatedAt عند كل حفظ
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity &&
                       e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                ((BaseEntity)entry.Entity).UpdatedAt = DateTime.Now;
            }
        }
    }
}
