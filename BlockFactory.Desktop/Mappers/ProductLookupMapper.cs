using BlockFactory.Core.Models.Products;
using BlockFactory.Desktop.ViewModels.Orders;

namespace BlockFactory.Desktop.Mappers
{
    public static class ProductLookupMapper
    {
        public static ProductLookupDto ToLookupDto(Product p)
            => new()
            {
                Id = p.Id,
                Name = p.Name,
                TypeName = p.ProductType?.Name ?? string.Empty,
                PriceMin = p.PriceMin,
                PriceMax = p.PriceMax,
                DefaultPrice = p.DefaultPrice,
                AvailableStock = p.Stock?.QuantityAvailable ?? 0
            };
    }
}
