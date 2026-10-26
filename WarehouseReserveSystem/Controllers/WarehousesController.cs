using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseReserveSystem.Services;

namespace WarehouseReserveSystem.Controllers;

[Authorize]
public class WarehousesController(IWarehouseService warehouseService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var warehouses = await warehouseService.GetAllWarehousesAsync();
        return View(warehouses);
    }

    public async Task<IActionResult> Details(int id)
    {
        var warehouse = await warehouseService.GetWarehouseByIdAsync(id);
        if (warehouse is null) return NotFound();

        return View(warehouse);
    }
}
