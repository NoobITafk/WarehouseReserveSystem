using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WarehouseReserveSystem.Data;
using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.Tests;

public sealed class TestDbContextScope : IAsyncDisposable
{
    public ApplicationDbContext Context { get; }
    private readonly SqliteConnection _connection;

    public TestDbContextScope(ApplicationDbContext context, SqliteConnection connection)
    {
        Context = context;
        _connection = connection;
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

public static class TestDbContextFactory
{
    public static async Task<TestDbContextScope> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new TestDbContextScope(context, connection);
    }

    public static async Task<(Category Category, Supplier Supplier, Warehouse Warehouse, StorageZone Zone, StorageLocation Location1, StorageLocation Location2)> CreateBaseHierarchyAsync(ApplicationDbContext context)
    {
        var cat = new Category { Name = "Загальна категорія", Code = "GEN" };
        var sup = new Supplier { Name = "Основний постачальник", TaxNumber = "12345678" };
        var wh = new Warehouse { Name = "Центральний хаб", Code = "WH-MAIN", TotalAreaSqM = 1500m };
        context.Categories.Add(cat);
        context.Suppliers.Add(sup);
        context.Warehouses.Add(wh);
        await context.SaveChangesAsync();

        var zone = new StorageZone { WarehouseId = wh.Id, Name = "Зона зберігання", Code = "Z-MAIN" };
        context.StorageZones.Add(zone);
        await context.SaveChangesAsync();

        var loc1 = new StorageLocation { StorageZoneId = zone.Id, Code = "A-01-01-A", Aisle = "1", Rack = "1", Shelf = "A", Bin = "1" };
        var loc2 = new StorageLocation { StorageZoneId = zone.Id, Code = "A-01-02-B", Aisle = "1", Rack = "1", Shelf = "B", Bin = "2" };
        context.StorageLocations.AddRange(loc1, loc2);
        await context.SaveChangesAsync();

        return (cat, sup, wh, zone, loc1, loc2);
    }
}
