using WarehouseReserveSystem.Models;
using WarehouseReserveSystem.Services;
using Xunit;

namespace WarehouseReserveSystem.Tests;

public class ProductServiceTests
{
    private class MockAuditService : IAuditService
    {
        public Task LogAsync(string action, string entityName, string? entityId, string details, string? staffLogin = null)
            => Task.CompletedTask;
    }

    [Fact]
    public async Task CreateProduct_EnforcesUniqueSku()
    {
        // Arrange
        await using var scope = await TestDbContextFactory.CreateAsync();
        var context = scope.Context;
        var service = new ProductService(context, new MockAuditService());

        var (cat, _, _, _, _, _) = await TestDbContextFactory.CreateBaseHierarchyAsync(context);

        var p1 = new Product { Sku = "SKU-DUP-1", Name = "Товар 1", Barcode = "111", CategoryId = cat.Id };
        await service.CreateProductAsync(p1, "tester");

        var p2 = new Product { Sku = "SKU-DUP-1", Name = "Товар 2", Barcode = "222", CategoryId = cat.Id };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateProductAsync(p2, "tester"));
    }

    [Fact]
    public async Task DeleteProduct_WithActiveStock_PerformsSoftDeactivate()
    {
        // Arrange
        await using var scope = await TestDbContextFactory.CreateAsync();
        var context = scope.Context;
        var service = new ProductService(context, new MockAuditService());

        var (cat, _, _, _, loc1, _) = await TestDbContextFactory.CreateBaseHierarchyAsync(context);

        var p = new Product { Sku = "SKU-DEL", Name = "Товар із залишком", Barcode = "333", CategoryId = cat.Id, IsActive = true };

        context.Products.Add(p);
        await context.SaveChangesAsync();

        // Has 5 units in stock
        context.StockItems.Add(new StockItem { ProductId = p.Id, StorageLocationId = loc1.Id, Quantity = 5 });
        await context.SaveChangesAsync();

        // Act: Request delete
        var deleted = await service.DeleteProductAsync(p.Id, "admin");

        // Assert: Product still exists in DB, but IsActive is false (soft delete)
        Assert.True(deleted);
        var existing = await context.Products.FindAsync(p.Id);
        Assert.NotNull(existing);
        Assert.False(existing.IsActive);
    }
}
