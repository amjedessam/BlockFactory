// BlockFactory.Data/UnitOfWork.cs

using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Repositories;
using BlockFactory.Data.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace BlockFactory.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        // ─── Specific Repositories (Lazy) ──────────
        private IOrderRepository? _orders;
        private ICustomerRepository? _customers;
        private IProductionRepository? _productions;
        private IInventoryRepository? _inventory;
        private IWorkerRepository? _workers;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        // ─── Specialized Repositories ───────────────
        public IOrderRepository Orders
            => _orders ??= new OrderRepository(_context);

        public ICustomerRepository Customers
            => _customers ??= new CustomerRepository(_context);

        public IProductionRepository Productions
            => _productions ??= new ProductionRepository(_context);

        public IInventoryRepository Inventory
            => _inventory ??= new InventoryRepository(_context);

        public IWorkerRepository Workers
            => _workers ??= new WorkerRepository(_context);

        // ─── Generic Repositories ───────────────────
        public IRepository<Core.Models.Auth.User> Users
            => new Repository<Core.Models.Auth.User>(_context);

        public IRepository<Core.Models.Auth.Role> Roles
            => new Repository<Core.Models.Auth.Role>(_context);

        public IRepository<Core.Models.Products.Product> Products
            => new Repository<Core.Models.Products.Product>(_context);

        public IRepository<Core.Models.Products.ProductType> ProductTypes
            => new Repository<Core.Models.Products.ProductType>(_context);

        public IRepository<Core.Models.Sales.Payment> Payments
            => new Repository<Core.Models.Sales.Payment>(_context);

        public IRepository<Core.Models.Sales.OrderItem> OrderItems
            => new Repository<Core.Models.Sales.OrderItem>(_context);

        public IRepository<Core.Models.Sales.Invoice> Invoices
            => new Repository<Core.Models.Sales.Invoice>(_context);

        public IRepository<Core.Models.Customers.Pledge> Pledges
            => new Repository<Core.Models.Customers.Pledge>(_context);

        public IRepository<Core.Models.Suppliers.Supplier> Suppliers
            => new Repository<Core.Models.Suppliers.Supplier>(_context);

        public IRepository<Core.Models.Suppliers.SupplierInvoice> SupplierInvoices
            => new Repository<Core.Models.Suppliers.SupplierInvoice>(_context);

        public IRepository<Core.Models.Suppliers.SupplierInvoiceItem> SupplierInvoiceItems
            => new Repository<Core.Models.Suppliers.SupplierInvoiceItem>(_context);

        public IRepository<Core.Models.Suppliers.SupplierPayment> SupplierPayments
            => new Repository<Core.Models.Suppliers.SupplierPayment>(_context);

        public IRepository<Core.Models.HR.MonthlySalary> Salaries
            => new Repository<Core.Models.HR.MonthlySalary>(_context);

        public IRepository<Core.Models.HR.Advance> Advances
            => new Repository<Core.Models.HR.Advance>(_context);

        public IRepository<Core.Models.HR.Deduction> Deductions
            => new Repository<Core.Models.HR.Deduction>(_context);

        public IRepository<Core.Models.Finance.Account> Accounts
            => new Repository<Core.Models.Finance.Account>(_context);

        public IRepository<Core.Models.Finance.JournalEntry> JournalEntries
            => new Repository<Core.Models.Finance.JournalEntry>(_context);

        public IRepository<Core.Models.Finance.Expense> Expenses
            => new Repository<Core.Models.Finance.Expense>(_context);

        public IRepository<Core.Models.Finance.ElectronicWallet> Wallets
            => new Repository<Core.Models.Finance.ElectronicWallet>(_context);

        public IRepository<Core.Models.Finance.ActivityLog> ActivityLogs
            => new Repository<Core.Models.Finance.ActivityLog>(_context);

        public IRepository<Core.Models.Inventory.RawMaterial> RawMaterials
            => new Repository<Core.Models.Inventory.RawMaterial>(_context);

        public IRepository<Core.Models.Inventory.RawMaterialTransaction> RawMaterialTransactions
            => new Repository<Core.Models.Inventory.RawMaterialTransaction>(_context);

        public IRepository<Core.Models.Inventory.InventoryTransaction> InventoryTransactions
            => new Repository<Core.Models.Inventory.InventoryTransaction>(_context);

        public IRepository<Core.Models.Production.ProductionMaterialUsage> ProductionMaterialUsages
            => new Repository<Core.Models.Production.ProductionMaterialUsage>(_context);

        public IRepository<Core.Models.Production.ProductionFormula> ProductionFormulas
            => new Repository<Core.Models.Production.ProductionFormula>(_context);

        // ─── Transaction Management ─────────────────
        public async Task<int> SaveChangesAsync()
            => await _context.SaveChangesAsync();

        public async Task BeginTransactionAsync()
            => _transaction = await _context.Database
                .BeginTransactionAsync();

        public async Task CommitAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Repositories;
using BlockFactory.Data.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace BlockFactory.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        // ─── Specific Repositories ─────────────────
        private IOrderRepository? _orders;
        private ICustomerRepository? _customers;
        private IProductionRepository? _productions;
        private IInventoryRepository? _inventory;
        private IWorkerRepository? _workers;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        // ─── Lazy Loading Repositories ─────────────

        public IOrderRepository Orders
            => _orders ??= new OrderRepository(_context);

        public ICustomerRepository Customers
            => _customers ??= new CustomerRepository(_context);

        public IProductionRepository Productions
            => _productions ??= new ProductionRepository(_context);

        public IInventoryRepository Inventory
            => _inventory ??= new InventoryRepository(_context);

        public IWorkerRepository Workers
            => _workers ??= new WorkerRepository(_context);

        // ─── Generic Repositories ──────────────────

        public IRepository<Core.Models.Auth.User> Users
            => new Repository<Core.Models.Auth.User>(_context);

        public IRepository<Core.Models.Auth.Role> Roles
            => new Repository<Core.Models.Auth.Role>(_context);

        public IRepository<Core.Models.Products.Product> Products
            => new Repository<Core.Models.Products.Product>(_context);

        public IRepository<Core.Models.Products.ProductType> ProductTypes
            => new Repository<Core.Models.Products.ProductType>(_context);

        public IRepository<Core.Models.Sales.Payment> Payments
            => new Repository<Core.Models.Sales.Payment>(_context);

        public IRepository<Core.Models.Sales.Invoice> Invoices
            => new Repository<Core.Models.Sales.Invoice>(_context);

        public IRepository<Core.Models.Customers.Pledge> Pledges
            => new Repository<Core.Models.Customers.Pledge>(_context);

        public IRepository<Core.Models.Suppliers.Supplier> Suppliers
            => new Repository<Core.Models.Suppliers.Supplier>(_context);

        public IRepository<Core.Models.Suppliers.SupplierInvoice> SupplierInvoices
            => new Repository<Core.Models.Suppliers.SupplierInvoice>(_context);

        public IRepository<Core.Models.HR.MonthlySalary> Salaries
            => new Repository<Core.Models.HR.MonthlySalary>(_context);

        public IRepository<Core.Models.HR.Advance> Advances
            => new Repository<Core.Models.HR.Advance>(_context);

        public IRepository<Core.Models.Finance.Account> Accounts
            => new Repository<Core.Models.Finance.Account>(_context);

        public IRepository<Core.Models.Finance.JournalEntry> JournalEntries
            => new Repository<Core.Models.Finance.JournalEntry>(_context);

        public IRepository<Core.Models.Finance.Expense> Expenses
            => new Repository<Core.Models.Finance.Expense>(_context);

        public IRepository<Core.Models.Finance.ElectronicWallet> Wallets
            => new Repository<Core.Models.Finance.ElectronicWallet>(_context);

        public IRepository<Core.Models.Finance.ActivityLog> ActivityLogs
            => new Repository<Core.Models.Finance.ActivityLog>(_context);

        public IRepository<Core.Models.Inventory.RawMaterial> RawMaterials
            => new Repository<Core.Models.Inventory.RawMaterial>(_context);

        // ─── Transaction Management ────────────────

        public async Task<int> SaveChangesAsync()
            => await _context.SaveChangesAsync();

        public async Task BeginTransactionAsync()
            => _transaction = await _context.Database
                .BeginTransactionAsync();

        public async Task CommitAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
*/