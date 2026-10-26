using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WarehouseReserveSystem.Models;
using WarehouseReserveSystem.Services;
using WarehouseReserveSystem.ViewModels;

namespace WarehouseReserveSystem.Controllers;

[Authorize]
public class ProductsController(IProductService productService) : Controller
{
    public async Task<IActionResult> Index(int? categoryId, string? search)
    {
        var products = await productService.GetAllProductsAsync(categoryId, search);
        var categories = await productService.GetCategoriesAsync();

        ViewBag.Categories = categories;
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.SearchTerm = search;

        return View(products);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await productService.GetProductByIdAsync(id);
        if (product is null) return NotFound();

        return View(product);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Storekeeper")]
    public async Task<IActionResult> Create()
    {
        var categories = await productService.GetCategoriesAsync();
        var suppliers = await productService.GetSuppliersAsync();

        var model = new ProductFormViewModel
        {
            Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList(),
            Suppliers = suppliers.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Storekeeper")]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var categories = await productService.GetCategoriesAsync();
            var suppliers = await productService.GetSuppliersAsync();
            model.Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
            model.Suppliers = suppliers.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList();
            return View(model);
        }

        try
        {
            var product = new Product
            {
                Sku = model.Sku.Trim().ToUpper(),
                Name = model.Name.Trim(),
                Description = model.Description,
                Barcode = model.Barcode.Trim(),
                CategoryId = model.CategoryId,
                SupplierId = model.SupplierId,
                Unit = model.Unit,
                CostPrice = model.CostPrice,
                SellingPrice = model.SellingPrice,
                MinStockThreshold = model.MinStockThreshold,
                WeightKg = model.WeightKg,
                IsActive = model.IsActive
            };

            await productService.CreateProductAsync(product, User.Identity?.Name ?? "system");
            TempData["SuccessMessage"] = $"Товар '{product.Name}' успішно зареєстровано в системі.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var categories = await productService.GetCategoriesAsync();
            var suppliers = await productService.GetSuppliersAsync();
            model.Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
            model.Suppliers = suppliers.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList();
            return View(model);
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Storekeeper")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await productService.GetProductByIdAsync(id);
        if (product is null) return NotFound();

        var categories = await productService.GetCategoriesAsync();
        var suppliers = await productService.GetSuppliersAsync();

        var model = new ProductFormViewModel
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            Barcode = product.Barcode,
            CategoryId = product.CategoryId,
            SupplierId = product.SupplierId,
            Unit = product.Unit,
            CostPrice = product.CostPrice,
            SellingPrice = product.SellingPrice,
            MinStockThreshold = product.MinStockThreshold,
            WeightKg = product.WeightKg,
            IsActive = product.IsActive,
            Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList(),
            Suppliers = suppliers.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Storekeeper")]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            var categories = await productService.GetCategoriesAsync();
            var suppliers = await productService.GetSuppliersAsync();
            model.Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
            model.Suppliers = suppliers.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList();
            return View(model);
        }

        try
        {
            var product = new Product
            {
                Id = model.Id,
                Sku = model.Sku.Trim().ToUpper(),
                Name = model.Name.Trim(),
                Description = model.Description,
                Barcode = model.Barcode.Trim(),
                CategoryId = model.CategoryId,
                SupplierId = model.SupplierId,
                Unit = model.Unit,
                CostPrice = model.CostPrice,
                SellingPrice = model.SellingPrice,
                MinStockThreshold = model.MinStockThreshold,
                WeightKg = model.WeightKg,
                IsActive = model.IsActive
            };

            await productService.UpdateProductAsync(product, User.Identity?.Name ?? "system");
            TempData["SuccessMessage"] = $"Дані товару '{product.Name}' успішно оновлено.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var categories = await productService.GetCategoriesAsync();
            var suppliers = await productService.GetSuppliersAsync();
            model.Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
            model.Suppliers = suppliers.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await productService.DeleteProductAsync(id, User.Identity?.Name ?? "system");
        if (success)
        {
            TempData["SuccessMessage"] = "Товар або успішно видалено, або деактивовано (якщо є залишки).";
        }
        else
        {
            TempData["ErrorMessage"] = "Товар не знайдено.";
        }

        return RedirectToAction(nameof(Index));
    }
}
