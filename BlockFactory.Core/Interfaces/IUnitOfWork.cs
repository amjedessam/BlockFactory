// BlockFactory.Core/Interfaces/IUnitOfWork.cs
/*
using BlockFactory.Core.Interfaces.Repositories;

namespace BlockFactory.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // ─── Specialized Repositories ──────────────
        IOrderRepository Orders { get; }
        ICustomerRepository Customers { get; }
        IProductionRepository Productions { get; }
        IInventoryRepository Inventory { get; }
        IWorkerRepository Workers { get; }

        // ─── Auth ──────────────────────────────────
        IRepository<Core.Models.Auth.User> Users { get; }
        IRepository<Core.Models.Auth.Role> Roles { get; }

        // ─── Products ──────────────────────────────
        IRepository<Core.Models.Products.Product> Products { get; }
        IRepository<Core.Models.Products.ProductType> ProductTypes { get; }

        // ─── Sales ─────────────────────────────────
        IRepository<Core.Models.Sales.Payment> Payments { get; }
        IRepository<Core.Models.Sales.OrderItem> OrderItems { get; }
        IRepository<Core.Models.Sales.Invoice> Invoices { get; }

        // ─── Customers ─────────────────────────────
        IRepository<Core.Models.Customers.Pledge> Pledges { get; }

        // ─── Suppliers ─────────────────────────────
        IRepository<Core.Models.Suppliers.Supplier> Suppliers { get; }
        IRepository<Core.Models.Suppliers.SupplierInvoice> SupplierInvoices { get; }
        IRepository<Core.Models.Suppliers.SupplierInvoiceItem> SupplierInvoiceItems { get; }
        IRepository<Core.Models.Suppliers.SupplierPayment> SupplierPayments { get; }

        // ─── HR ────────────────────────────────────
        IRepository<Core.Models.HR.MonthlySalary> Salaries { get; }
        IRepository<Core.Models.HR.Advance> Advances { get; }
        IRepository<Core.Models.HR.Deduction> Deductions { get; }

        // ─── Finance ───────────────────────────────
        IRepository<Core.Models.Finance.Account> Accounts { get; }
        IRepository<Core.Models.Finance.JournalEntry> JournalEntries { get; }
        IRepository<Core.Models.Finance.Expense> Expenses { get; }
        IRepository<Core.Models.Finance.ElectronicWallet> Wallets { get; }
        IRepository<Core.Models.Finance.ActivityLog> ActivityLogs { get; }

        // ─── Inventory ─────────────────────────────
        IRepository<Core.Models.Inventory.RawMaterial> RawMaterials { get; }
        IRepository<Core.Models.Inventory.RawMaterialTransaction> RawMaterialTransactions { get; }
        IRepository<Core.Models.Inventory.InventoryTransaction> InventoryTransactions { get; }

        // ─── Production ────────────────────────────
        IRepository<Core.Models.Production.ProductionMaterialUsage> ProductionMaterialUsages { get; }
        IRepository<Core.Models.Production.ProductionFormula> ProductionFormulas { get; }

        // ─── Save & Transactions ───────────────────
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Interfaces.Repositories;

namespace BlockFactory.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // ─── Repositories ──────────────────────────
        IOrderRepository Orders { get; }
        ICustomerRepository Customers { get; }
        IProductionRepository Productions { get; }
        IInventoryRepository Inventory { get; }
        IWorkerRepository Workers { get; }

        IRepository<BlockFactory.Core.Models.Auth.User> Users { get; }
        IRepository<BlockFactory.Core.Models.Auth.Role> Roles { get; }
        IRepository<BlockFactory.Core.Models.Products.Product> Products { get; }
        IRepository<BlockFactory.Core.Models.Products.ProductType> ProductTypes { get; }
        IRepository<BlockFactory.Core.Models.Sales.Payment> Payments { get; }
        IRepository<BlockFactory.Core.Models.Sales.Invoice> Invoices { get; }
        IRepository<BlockFactory.Core.Models.Customers.Pledge> Pledges { get; }
        IRepository<BlockFactory.Core.Models.Suppliers.Supplier> Suppliers { get; }
        IRepository<BlockFactory.Core.Models.Suppliers.SupplierInvoice> SupplierInvoices { get; }
        IRepository<BlockFactory.Core.Models.HR.MonthlySalary> Salaries { get; }
        IRepository<BlockFactory.Core.Models.HR.Advance> Advances { get; }
        IRepository<BlockFactory.Core.Models.Finance.Account> Accounts { get; }
        IRepository<BlockFactory.Core.Models.Finance.JournalEntry> JournalEntries { get; }
        IRepository<BlockFactory.Core.Models.Finance.Expense> Expenses { get; }
        IRepository<BlockFactory.Core.Models.Finance.ElectronicWallet> Wallets { get; }
        IRepository<BlockFactory.Core.Models.Finance.ActivityLog> ActivityLogs { get; }
        IRepository<BlockFactory.Core.Models.Inventory.RawMaterial> RawMaterials { get; }

        // ─── Save ──────────────────────────────────
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
*/


// BlockFactory.Core/Interfaces/IUnitOfWork.cs

using BlockFactory.Core.Interfaces.Repositories;
using BlockFactory.Core.Models.Reservations;

namespace BlockFactory.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // ─── Specialized Repositories ───────────────
        IOrderRepository Orders { get; }
        ICustomerRepository Customers { get; }
        IProductionRepository Productions { get; }
        IInventoryRepository Inventory { get; }
        IWorkerRepository Workers { get; }

        // ─── Auth ───────────────────────────────────
        IRepository<Core.Models.Auth.User> Users { get; }
        IRepository<Core.Models.Auth.Role> Roles { get; }

        // ─── Products ───────────────────────────────
        IRepository<Core.Models.Products.Product> Products { get; }
        IRepository<Core.Models.Products.ProductType> ProductTypes { get; }

        // ─── Sales ──────────────────────────────────
        IRepository<Core.Models.Sales.Payment> Payments { get; }
        IRepository<Core.Models.Sales.OrderItem> OrderItems { get; }
        IRepository<Core.Models.Sales.Invoice> Invoices { get; }

        // ─── Customers ──────────────────────────────
        IRepository<Core.Models.Customers.Pledge> Pledges { get; }

        // ─── Reservations ───────────────────────────
        IRepository<Reservation> Reservations { get; }
        IRepository<ReservationItem> ReservationItems { get; }
        IRepository<PriceSnapshot> PriceSnapshots { get; }
        IRepository<WithdrawalTransaction> WithdrawalTransactions { get; }
        IRepository<WithdrawalItem> WithdrawalItems { get; }

        // ─── Suppliers ──────────────────────────────
        IRepository<Core.Models.Suppliers.Supplier> Suppliers { get; }
        IRepository<Core.Models.Suppliers.SupplierInvoice> SupplierInvoices { get; }
        IRepository<Core.Models.Suppliers.SupplierInvoiceItem> SupplierInvoiceItems { get; }
        IRepository<Core.Models.Suppliers.SupplierPayment> SupplierPayments { get; }

        // ─── HR ─────────────────────────────────────
        IRepository<Core.Models.HR.MonthlySalary> Salaries { get; }
        IRepository<Core.Models.HR.Advance> Advances { get; }
        IRepository<Core.Models.HR.Deduction> Deductions { get; }

        // ─── Finance ────────────────────────────────
        IRepository<Core.Models.Finance.Account> Accounts { get; }
        IRepository<Core.Models.Finance.JournalEntry> JournalEntries { get; }
        IRepository<Core.Models.Finance.Expense> Expenses { get; }
        IRepository<Core.Models.Finance.ElectronicWallet> Wallets { get; }
        IRepository<Core.Models.Finance.ActivityLog> ActivityLogs { get; }

        // ─── Inventory ──────────────────────────────
        IRepository<Core.Models.Inventory.RawMaterial> RawMaterials { get; }
        IRepository<Core.Models.Inventory.RawMaterialTransaction> RawMaterialTransactions { get; }
        IRepository<Core.Models.Inventory.InventoryTransaction> InventoryTransactions { get; }

        // ─── Production ─────────────────────────────
        IRepository<Core.Models.Production.ProductionMaterialUsage> ProductionMaterialUsages { get; }
        IRepository<Core.Models.Production.ProductionFormula> ProductionFormulas { get; }

        // ─── Save & Transactions ────────────────────
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
