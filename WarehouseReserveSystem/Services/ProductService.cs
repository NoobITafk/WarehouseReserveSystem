using Microsoft.EntityFrameworkCore;
using WarehouseReserveSystem.Data;
using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.Services;

public class ProductService(ApplicationDbContext context, IAuditService auditService) : IProductService
{
    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(int? categoryId = null, string? search = null)
    {
        var query = context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.StockItems)
            .AsNoTracking();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(term) ||
                p.Sku.ToLower().Contains(term) ||
                p.Barcode.ToLower().Contains(term));
        }

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.StockItems)
                .ThenInclude(s => s.StorageLocation!)
                    .ThenInclude(l => l.StorageZone!)
                        .ThenInclude(z => z.Warehouse)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetProductBySkuAsync(string sku)
    {
        return await context.Products
            .Include(p => p.Category)
            .Include(p => p.StockItems)
            .FirstOrDefaultAsync(p => p.Sku.ToLower() == sku.Trim().ToLower());
    }

    public async Task<Product> CreateProductAsync(Product product, string staffLogin)
    {
        // Validate SKU uniqueness
        var existing = await context.Products.AnyAsync(p => p.Sku.ToLower() == product.Sku.Trim().ToLower());
        if (existing)
        {
            throw new InvalidOperationException($"Товар з артикулом '{product.Sku}' вже існує в системі.");
        }

        product.CreatedAt = DateTime.UtcNow;
        context.Products.Add(product);
        await context.SaveChangesAsync();

        await auditService.LogAsync(
            "CreateProduct",
            "Product",
            product.Id.ToString(),
            $"Створено новий товар: {product.Name} (SKU: {product.Sku})",
            staffLogin
        );

        return product;
    }

    public async Task<Product> UpdateProductAsync(Product product, string staffLogin)
    {
        var existing = await context.Products.FindAsync(product.Id)
            ?? throw new InvalidOperationException($"Товар з ID {product.Id} не знайдено.");

        // Check if SKU changed and conflicts
        if (existing.Sku != product.Sku)
        {
            var skuConflict = await context.Products.AnyAsync(p => p.Id != product.Id && p.Sku.ToLower() == product.Sku.Trim().ToLower());
            if (skuConflict)
            {
                throw new InvalidOperationException($"Артикул '{product.Sku}' вже використовується іншим товаром.");
            }
        }

        existing.Sku = product.Sku;
        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.Barcode = product.Barcode;
        existing.CategoryId = product.CategoryId;
        existing.SupplierId = product.SupplierId;
        existing.Unit = product.Unit;
        existing.CostPrice = product.CostPrice;
        existing.SellingPrice = product.SellingPrice;
        existing.MinStockThreshold = product.MinStockThreshold;
        existing.WeightKg = product.WeightKg;
        existing.IsActive = product.IsActive;

        await context.SaveChangesAsync();

        await auditService.LogAsync(
            "UpdateProduct",
            "Product",
            existing.Id.ToString(),
            $"Оновлено параметри товару: {existing.Name} (SKU: {existing.Sku})",
            staffLogin
        );

        return existing;
    }

    public async Task<bool> DeleteProductAsync(int id, string staffLogin)
    {
        var product = await context.Products
            .Include(p => p.StockItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return false;

        // If product has non-zero stock, do soft-delete (deactivate) rather than deleting
        var totalStock = product.StockItems.Sum(s => s.Quantity);
        if (totalStock > 0)
        {
            product.IsActive = false;
            await context.SaveChangesAsync();
            await auditService.LogAsync(
                "DeactivateProduct",
                "Product",
                id.ToString(),
                $"Товар '{product.Name}' деактивовано (наявний залишок: {totalStock} {product.Unit})",
                staffLogin
            );
            return true;
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync();

        await auditService.LogAsync(
            "DeleteProduct",
            "Product",
            id.ToString(),
            $"Видалено товар: {product.Name} (SKU: {product.Sku})",
            staffLogin
        );

        return true;
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync()
    {
        return await context.Categories
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Supplier>> GetSuppliersAsync()
    {
        return await context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync();
    }
}
