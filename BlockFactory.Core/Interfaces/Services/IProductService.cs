/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockFactory.Core.Models.Products;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
    }
}*/
// BlockFactory.Core/Interfaces/Services/IProductService.cs

using BlockFactory.Core.DTOs.Products;
using BlockFactory.Core.Models.Products;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);

        // ← هذه كانت مفقودة وتسبب خطأ في NewOrderViewModel
        Task<IEnumerable<Product>> GetActiveProductsAsync();
    }
}