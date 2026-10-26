using WarehouseReserveSystem.Models;
using WarehouseReserveSystem.Services;

namespace WarehouseReserveSystem.ViewModels;

public class DashboardViewModel
{
    public decimal TotalStockValuation { get; set; }
    public int TotalUnitsInStock { get; set; }
    public int TotalActiveSkus { get; set; }
    public int LowStockCount { get; set; }
    public int WarehousesCount { get; set; }
    public int StorageLocationsCount { get; set; }

    public IReadOnlyList<StockMovement> RecentMovements { get; set; } = [];
    public IReadOnlyList<ProductStockSummary> LowStockItems { get; set; } = [];
    public IReadOnlyList<WarehouseOccupancySummary> Warehouses { get; set; } = [];
}

public record WarehouseOccupancySummary(
    int WarehouseId, 
    string Name, 
    string Code, 
    int TotalLocations, 
    int OccupiedLocations, 
    int TotalItemsCount,
    decimal TotalValuation);
