using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseReserveSystem.Services;

namespace WarehouseReserveSystem.Controllers;

[Authorize]
public class StockController(IInventoryService inventoryService, IWarehouseService warehouseService, IProductService productService) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, int? categoryId, string? search)
    {
        var stockBalances = await inventoryService.GetStockBalancesAsync(warehouseId, categoryId, search);
        var warehouses = await warehouseService.GetAllWarehousesAsync();
        var categories = await productService.GetCategoriesAsync();

        ViewBag.Warehouses = warehouses;
        ViewBag.Categories = categories;
        ViewBag.SelectedWarehouseId = warehouseId;
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.SearchTerm = search;

        ViewBag.TotalUnits = stockBalances.Sum(s => s.Quantity);
        ViewBag.TotalValuation = stockBalances.Sum(s => s.Quantity * (s.Product?.CostPrice ?? 0m));

        return View(stockBalances);
    }

    public async Task<IActionResult> Alerts()
    {
        var alerts = await inventoryService.GetLowStockAlertsAsync();
        return View(alerts);
    }
}
