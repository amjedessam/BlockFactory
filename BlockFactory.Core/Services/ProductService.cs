using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Products;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Core.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;

        public ProductService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
            => await _uow.Products.Query()
                .Include(p => p.ProductType)
                .Include(p => p.Stock)
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Name)
                .ToListAsync();

        public async Task<Product?> GetByIdAsync(int id)
            => await _uow.Products.Query()
                .Include(p => p.ProductType)
                .Include(p => p.Stock)
                .FirstOrDefaultAsync(p =>
                    p.Id == id && !p.IsDeleted);

        public async Task<IEnumerable<Product>> GetActiveProductsAsync()
            => await _uow.Products.Query()
                .Include(p => p.ProductType)
                .Include(p => p.Stock)
                .Where(p => p.IsActive && !p.IsDeleted)
                .OrderBy(p => p.Name)
                .ToListAsync();
    }
}
