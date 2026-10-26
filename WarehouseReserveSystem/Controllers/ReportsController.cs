using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseReserveSystem.Services;

namespace WarehouseReserveSystem.Controllers;

[Authorize(Roles = "Admin,Auditor")]
public class ReportsController(
    IInventoryService inventoryService, 
    IReportExportService reportExportService) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ExportStockBalancesCsv()
    {
        var balances = await inventoryService.GetStockBalancesAsync();
        var csvBytes = reportExportService.ExportStockBalancesToCsv(balances);
        var filename = $"StockBalances_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";

        return File(csvBytes, "text/csv; charset=utf-8", filename);
    }

    [HttpGet]
    public async Task<IActionResult> ExportMovementsCsv()
    {
        var movements = await inventoryService.GetRecentMovementsAsync(500);
        var csvBytes = reportExportService.ExportMovementsToCsv(movements);
        var filename = $"StockMovements_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";

        return File(csvBytes, "text/csv; charset=utf-8", filename);
    }
}
