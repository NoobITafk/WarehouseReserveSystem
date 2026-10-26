using WarehouseReserveSystem.Data;
using WarehouseReserveSystem.Models;
using WarehouseReserveSystem.Services;
using Xunit;

namespace WarehouseReserveSystem.Tests;

public class WarehouseServiceTests
{
    [Fact]
    public async Task GetAllWarehousesAsync_ReturnsWarehousesWithZonesAndLocations()
    {
        // Arrange
        await using var scope = await TestDbContextFactory.CreateAsync();
        var context = scope.Context;

        var (_, _, wh, _, _, _) = await TestDbContextFactory.CreateBaseHierarchyAsync(context);

        var service = new WarehouseService(context);

        // Act
        var result = await service.GetAllWarehousesAsync();

        // Assert
        Assert.NotEmpty(result);
        var foundWh = result.FirstOrDefault(w => w.Id == wh.Id);
        Assert.NotNull(foundWh);
        Assert.NotEmpty(foundWh.Zones);
    }

    [Fact]
    public async Task GetLocationsByZoneIdAsync_ReturnsLocationsForZone()
    {
        // Arrange
        await using var scope = await TestDbContextFactory.CreateAsync();
        var context = scope.Context;

        var (_, _, _, zone, loc1, loc2) = await TestDbContextFactory.CreateBaseHierarchyAsync(context);

        var service = new WarehouseService(context);

        // Act
        var locations = await service.GetLocationsByZoneIdAsync(zone.Id);

        // Assert
        Assert.Equal(2, locations.Count);
        Assert.Contains(locations, l => l.Id == loc1.Id);
        Assert.Contains(locations, l => l.Id == loc2.Id);
    }

    [Fact]
    public async Task GetWarehouseByIdAsync_ReturnsNullForNonExistentId()
    {
        // Arrange
        await using var scope = await TestDbContextFactory.CreateAsync();
        var context = scope.Context;
        var service = new WarehouseService(context);

        // Act
        var result = await service.GetWarehouseByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }
}
