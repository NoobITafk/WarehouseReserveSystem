using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllProductsAsync(int? categoryId = null, string? search = null);
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product?> GetProductBySkuAsync(string sku);
    Task<Product> CreateProductAsync(Product product, string staffLogin);
    Task<Product> UpdateProductAsync(Product product, string staffLogin);
    Task<bool> DeleteProductAsync(int id, string staffLogin);

    Task<IReadOnlyList<Category>> GetCategoriesAsync();
    Task<IReadOnlyList<Supplier>> GetSuppliersAsync();
}
