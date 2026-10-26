using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WarehouseReserveSystem.Models;
using WarehouseReserveSystem.Services;
using WarehouseReserveSystem.ViewModels;

namespace WarehouseReserveSystem.Controllers;

[Authorize]
public class MovementsController(
    IInventoryService inventoryService,
    IProductService productService,
    IWarehouseService warehouseService,
    IWaybillPdfService waybillPdfService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var movements = await inventoryService.GetRecentMovementsAsync(50);
        return View(movements);
    }

    public async Task<IActionResult> Details(int id)
    {
        var movement = await inventoryService.GetMovementByIdAsync(id);
        if (movement is null) return NotFound();

        return View(movement);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Storekeeper")]
    public async Task<IActionResult> Receipt()
    {
        var suppliers = await productService.GetSuppliersAsync();
        var locations = await warehouseService.GetAllLocationsAsync();
        var products = await productService.GetAllProductsAsync();

        var model = new ReceiptFormViewModel
        {
            Suppliers = suppliers.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList(),
            Locations = locations.Select(l => new SelectListItem($"{l.StorageZone?.Warehouse?.Code} · {l.Code}", l.Id.ToString())).ToList(),
            Products = products.Select(p => new SelectListItem($"{p.Sku} · {p.Name} (собівартість: {p.CostPrice:N0} грн)", p.Id.ToString())).ToList(),
            UnitPrice = products.FirstOrDefault()?.CostPrice ?? 100m
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Storekeeper")]
    public async Task<IActionResult> Receipt(ReceiptFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateReceiptDropdowns(model);
            return View(model);
        }

        try
        {
            var items = new List<(int productId, int quantity, decimal unitPrice, string? batchNumber)>
            {
                (model.ProductId, model.Quantity, model.UnitPrice, model.BatchNumber)
            };

            var staffLogin = User.Identity?.Name ?? "system";
            var movement = await inventoryService.ExecuteReceiptAsync(
                model.SupplierId, 
                model.DestinationLocationId, 
                items, 
                model.Notes, 
                staffLogin
            );

            TempData["SuccessMessage"] = $"Прибуткову накладну {movement.DocumentNumber} успішно проведено!";
            return RedirectToAction(nameof(Details), new { id = movement.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateReceiptDropdowns(model);
            return View(model);
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Storekeeper")]
    public async Task<IActionResult> Shipment()
    {
        var locations = await warehouseService.GetAllLocationsAsync();
        var products = await productService.GetAllProductsAsync();

        var model = new ShipmentFormViewModel
        {
            Locations = locations.Select(l => new SelectListItem($"{l.StorageZone?.Warehouse?.Code} · {l.Code}", l.Id.ToString())).ToList(),
            Products = products.Select(p => new SelectListItem($"{p.Sku} · {p.Name} (ціна: {p.SellingPrice:N0} грн)", p.Id.ToString())).ToList(),
            UnitPrice = products.FirstOrDefault()?.SellingPrice ?? 150m
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Storekeeper")]
    public async Task<IActionResult> Shipment(ShipmentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateShipmentDropdowns(model);
            return View(model);
        }

        try
        {
            var items = new List<(int productId, int quantity, decimal unitPrice)>
            {
                (model.ProductId, model.Quantity, model.UnitPrice)
            };

            var staffLogin = User.Identity?.Name ?? "system";
            var movement = await inventoryService.ExecuteShipmentAsync(
                model.CustomerName, 
                model.SourceLocationId, 
                items, 
                model.Notes, 
                staffLogin
            );

            TempData["SuccessMessage"] = $"Видаткову накладну {movement.DocumentNumber} успішно проведено!";
            return RedirectToAction(nameof(Details), new { id = movement.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateShipmentDropdowns(model);
            return View(model);
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Storekeeper")]
    public async Task<IActionResult> Transfer()
    {
        var locations = await warehouseService.GetAllLocationsAsync();
        var products = await productService.GetAllProductsAsync();

        var model = new TransferFormViewModel
        {
            Products = products.Select(p => new SelectListItem($"{p.Sku} · {p.Name}", p.Id.ToString())).ToList(),
            Locations = locations.Select(l => new SelectListItem($"{l.StorageZone?.Warehouse?.Code} · {l.Code}", l.Id.ToString())).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Storekeeper")]
    public async Task<IActionResult> Transfer(TransferFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateTransferDropdowns(model);
            return View(model);
        }

        try
        {
            var staffLogin = User.Identity?.Name ?? "system";
            var movement = await inventoryService.ExecuteTransferAsync(
                model.SourceLocationId,
                model.DestinationLocationId,
                model.ProductId,
                model.Quantity,
                model.Notes,
                staffLogin
            );

            TempData["SuccessMessage"] = $"Внутрішнє переміщення {movement.DocumentNumber} успішно зафіксовано!";
            return RedirectToAction(nameof(Details), new { id = movement.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateTransferDropdowns(model);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Pdf(int id)
    {
        var movement = await inventoryService.GetMovementByIdAsync(id);
        if (movement is null) return NotFound();

        var pdfBytes = waybillPdfService.GenerateWaybillPdf(movement);
        return File(pdfBytes, "application/pdf", $"Waybill_{movement.DocumentNumber}.pdf");
    }

    private async Task PopulateReceiptDropdowns(ReceiptFormViewModel model)
    {
        var suppliers = await productService.GetSuppliersAsync();
        var locations = await warehouseService.GetAllLocationsAsync();
        var products = await productService.GetAllProductsAsync();

        model.Suppliers = suppliers.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList();
        model.Locations = locations.Select(l => new SelectListItem($"{l.StorageZone?.Warehouse?.Code} · {l.Code}", l.Id.ToString())).ToList();
        model.Products = products.Select(p => new SelectListItem($"{p.Sku} · {p.Name}", p.Id.ToString())).ToList();
    }

    private async Task PopulateShipmentDropdowns(ShipmentFormViewModel model)
    {
        var locations = await warehouseService.GetAllLocationsAsync();
        var products = await productService.GetAllProductsAsync();

        model.Locations = locations.Select(l => new SelectListItem($"{l.StorageZone?.Warehouse?.Code} · {l.Code}", l.Id.ToString())).ToList();
        model.Products = products.Select(p => new SelectListItem($"{p.Sku} · {p.Name}", p.Id.ToString())).ToList();
    }

    private async Task PopulateTransferDropdowns(TransferFormViewModel model)
    {
        var locations = await warehouseService.GetAllLocationsAsync();
        var products = await productService.GetAllProductsAsync();

        model.Locations = locations.Select(l => new SelectListItem($"{l.StorageZone?.Warehouse?.Code} · {l.Code}", l.Id.ToString())).ToList();
        model.Products = products.Select(p => new SelectListItem($"{p.Sku} · {p.Name}", p.Id.ToString())).ToList();
    }
}
