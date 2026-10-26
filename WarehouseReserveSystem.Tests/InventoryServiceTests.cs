using Microsoft.EntityFrameworkCore;
using WarehouseReserveSystem.Data;
using WarehouseReserveSystem.Models;
using WarehouseReserveSystem.Services;
using Xunit;

namespace WarehouseReserveSystem.Tests;

public class InventoryServiceTests
{
    private class MockAuditService : IAuditService
    {
        public List<string> LoggedActions { get; } = [];

        public Task LogAsync(string action, string entityName, string? entityId, string details, string? staffLogin = null)
        {
            LoggedActions.Add($"{action}:{entityName}:{details}");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ExecuteReceipt_IncreasesStockAndCreatesMovement()
    {
        // Arrange
        await using var scope = await TestDbContextFactory.CreateAsync();
        var context = scope.Context;
        var audit = new MockAuditService();

        var (cat, sup, _, _, loc1, _) = await TestDbContextFactory.CreateBaseHierarchyAsync(context);

        var prod = new Product
        {
            Sku = "SKU-TEST-01",
            Name = "Контролер",
            Barcode = "111111",
            CategoryId = cat.Id,
            CostPrice = 1000m,
            SellingPrice = 1500m
        };
        context.Products.Add(prod);
        await context.SaveChangesAsync();

        var service = new InventoryService(context, audit);

        var items = new List<(int productId, int quantity, decimal unitPrice, string? batchNumber)>
        {
            (prod.Id, 15, 1000m, "BATCH-001")
        };

        // Act
        var doc = await service.ExecuteReceiptAsync(sup.Id, loc1.Id, items, "Первинний прихід", "tester");

        // Assert
        Assert.NotNull(doc);
        Assert.StartsWith("DOC-REC-", doc.DocumentNumber);
        Assert.Equal(StockMovementStatus.Completed, doc.Status);

        var stock = await context.StockItems.FirstOrDefaultAsync(s => s.ProductId == prod.Id && s.StorageLocationId == loc1.Id);
        Assert.NotNull(stock);
        Assert.Equal(15, stock.Quantity);
        Assert.Equal("BATCH-001", stock.BatchNumber);

        Assert.Contains(audit.LoggedActions, a => a.StartsWith("StockReceipt:StockMovement"));
    }

    [Fact]
    public async Task ExecuteShipment_DecreasesStockAndThrowsIfInsufficient()
    {
        // Arrange
        await using var scope = await TestDbContextFactory.CreateAsync();
        var context = scope.Context;
        var audit = new MockAuditService();

        var (cat, _, _, _, loc1, _) = await TestDbContextFactory.CreateBaseHierarchyAsync(context);

        var prod = new Product
        {
            Sku = "SKU-02",
            Name = "Перфоратор",
            Barcode = "222222",
            CategoryId = cat.Id,
            CostPrice = 500m,
            SellingPrice = 800m
        };
        context.Products.Add(prod);
        await context.SaveChangesAsync();

        // Seed with 10 units
        context.StockItems.Add(new StockItem { ProductId = prod.Id, StorageLocationId = loc1.Id, Quantity = 10 });
        await context.SaveChangesAsync();

        var service = new InventoryService(context, audit);

        // Act 1: Valid shipment of 4 units
        var items = new List<(int productId, int quantity, decimal unitPrice)>
        {
            (prod.Id, 4, 800m)
        };
        var doc = await service.ExecuteShipmentAsync("ТОВ Клієнт", loc1.Id, items, "Замовлення 1", "tester");

        // Assert 1
        Assert.NotNull(doc);
        var stock = await context.StockItems.FirstAsync(s => s.ProductId == prod.Id && s.StorageLocationId == loc1.Id);
        Assert.Equal(6, stock.Quantity);

        // Act & Assert 2: Over-shipment of 7 units (only 6 available) must throw!
        var excessItems = new List<(int productId, int quantity, decimal unitPrice)>
        {
            (prod.Id, 7, 800m)
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteShipmentAsync("ТОВ Клієнт", loc1.Id, excessItems, null, "tester"));
    }

    [Fact]
    public async Task ExecuteTransfer_AtomicallyMovesStockBetweenLocations()
    {
        // Arrange
        await using var scope = await TestDbContextFactory.CreateAsync();
        var context = scope.Context;
        var audit = new MockAuditService();

        var (cat, _, _, _, locSrc, locDst) = await TestDbContextFactory.CreateBaseHierarchyAsync(context);

        var prod = new Product
        {
            Sku = "SKU-03",
            Name = "Кабель",
            Barcode = "333333",
            CategoryId = cat.Id,
            CostPrice = 200m,
            SellingPrice = 300m
        };
        context.Products.Add(prod);
        await context.SaveChangesAsync();

        // Put 20 units in source location
        context.StockItems.Add(new StockItem { ProductId = prod.Id, StorageLocationId = locSrc.Id, Quantity = 20, BatchNumber = "B-01" });
        await context.SaveChangesAsync();

        var service = new InventoryService(context, audit);

        // Act: Transfer 8 units from locSrc to locDst
        var doc = await service.ExecuteTransferAsync(locSrc.Id, locDst.Id, prod.Id, 8, "Планова ротація", "storekeeper");

        // Assert
        Assert.NotNull(doc);
        var srcStock = await context.StockItems.FirstAsync(s => s.ProductId == prod.Id && s.StorageLocationId == locSrc.Id);
        var dstStock = await context.StockItems.FirstAsync(s => s.ProductId == prod.Id && s.StorageLocationId == locDst.Id);

        Assert.Equal(12, srcStock.Quantity);
        Assert.Equal(8, dstStock.Quantity);
        Assert.Equal("B-01", dstStock.BatchNumber);
    }

    [Fact]
    public async Task GetLowStockAlerts_CorrectlyIdentifiesDeficitProducts()
    {
        // Arrange
        await using var scope = await TestDbContextFactory.CreateAsync();
        var context = scope.Context;
        var audit = new MockAuditService();

        var (cat, _, _, _, loc1, _) = await TestDbContextFactory.CreateBaseHierarchyAsync(context);

        var pAdequate = new Product { Sku = "SKU-OK", Name = "Достатній", Barcode = "1", CategoryId = cat.Id, MinStockThreshold = 10 };
        var pDeficit = new Product { Sku = "SKU-LOW", Name = "Дефіцитний", Barcode = "2", CategoryId = cat.Id, MinStockThreshold = 20 };

        context.Products.AddRange(pAdequate, pDeficit);
        await context.SaveChangesAsync();

        // 15 units of pAdequate (threshold 10 -> OK)
        context.StockItems.Add(new StockItem { ProductId = pAdequate.Id, StorageLocationId = loc1.Id, Quantity = 15 });
        // 5 units of pDeficit (threshold 20 -> DEFICIT!)
        context.StockItems.Add(new StockItem { ProductId = pDeficit.Id, StorageLocationId = loc1.Id, Quantity = 5 });
        await context.SaveChangesAsync();

        var service = new InventoryService(context, audit);

        // Act
        var alerts = await service.GetLowStockAlertsAsync();

        // Assert
        Assert.Single(alerts);
        Assert.Equal("SKU-LOW", alerts[0].Sku);
        Assert.Equal(5, alerts[0].CurrentStock);
        Assert.Equal(20, alerts[0].MinThreshold);
        Assert.True(alerts[0].IsCritical);
    }
}
