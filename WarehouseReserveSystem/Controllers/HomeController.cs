using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseReserveSystem.Services;
using WarehouseReserveSystem.ViewModels;

namespace WarehouseReserveSystem.Controllers;

[Authorize]
public class HomeController(IInventoryService inventoryService, IWarehouseService warehouseService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var totalValuation = await inventoryService.GetTotalValuationAsync();
        var totalUnits = await inventoryService.GetTotalUnitsCountAsync();
        var activeSkus = await inventoryService.GetTotalActiveSkuCountAsync();
        var lowStockAlerts = await inventoryService.GetLowStockAlertsAsync();
        var recentMovements = await inventoryService.GetRecentMovementsAsync(6);

        var warehouses = await warehouseService.GetAllWarehousesAsync();
        var allLocations = await warehouseService.GetAllLocationsAsync();

        var occupancySummaries = warehouses.Select(w =>
        {
            var whLocs = allLocations.Where(l => l.StorageZone?.WarehouseId == w.Id).ToList();
            var totalLocs = whLocs.Count;
            var occupiedLocs = whLocs.Count(l => l.StockItems.Any(s => s.Quantity > 0));
            var totalItems = whLocs.SelectMany(l => l.StockItems).Sum(s => s.Quantity);
            var valuation = whLocs.SelectMany(l => l.StockItems).Sum(s => s.Quantity * (s.Product?.CostPrice ?? 0m));

            return new WarehouseOccupancySummary(
                w.Id,
                w.Name,
                w.Code,
                totalLocs,
                occupiedLocs,
                totalItems,
                valuation
            );
        }).ToList();

        var model = new DashboardViewModel
        {
            TotalStockValuation = totalValuation,
            TotalUnitsInStock = totalUnits,
            TotalActiveSkus = activeSkus,
            LowStockCount = lowStockAlerts.Count,
            WarehousesCount = warehouses.Count,
            StorageLocationsCount = allLocations.Count,
            RecentMovements = recentMovements,
            LowStockItems = lowStockAlerts,
            Warehouses = occupancySummaries
        };

        return View(model);
    }
}
