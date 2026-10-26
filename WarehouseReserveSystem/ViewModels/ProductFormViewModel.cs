using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.ViewModels;

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть артикул товару (SKU).")]
    [Display(Name = "Артикул (SKU)")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть назву товару.")]
    [Display(Name = "Назва товару")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Опис / Характеристики")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Вкажіть штрих-код (EAN-13 або внутрішній).")]
    [Display(Name = "Штрих-код")]
    public string Barcode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Оберіть категорію товару.")]
    [Display(Name = "Категорія")]
    public int CategoryId { get; set; }

    [Display(Name = "Основний постачальник")]
    public int? SupplierId { get; set; }

    [Required(ErrorMessage = "Вкажіть одиницю виміру.")]
    [Display(Name = "Одиниця виміру")]
    public string Unit { get; set; } = "шт";

    [Required(ErrorMessage = "Вкажіть собівартість.")]
    [Range(0.01, 10000000, ErrorMessage = "Собівартість повинна бути більшою за нуль.")]
    [Display(Name = "Собівартість (грн)")]
    public decimal CostPrice { get; set; }

    [Required(ErrorMessage = "Вкажіть ціну продажу.")]
    [Range(0.01, 10000000, ErrorMessage = "Ціна продажу повинна бути більшою за нуль.")]
    [Display(Name = "Ціна продажу (грн)")]
    public decimal SellingPrice { get; set; }

    [Required(ErrorMessage = "Вкажіть мінімальний поріг залишку.")]
    [Range(0, 100000, ErrorMessage = "Поріг не може бути від'ємним.")]
    [Display(Name = "Критичний залишок (Min Stock)")]
    public int MinStockThreshold { get; set; } = 5;

    [Display(Name = "Вага (кг)")]
    public decimal WeightKg { get; set; } = 1.0m;

    [Display(Name = "Активний для продажу")]
    public bool IsActive { get; set; } = true;

    public List<SelectListItem> Categories { get; set; } = [];
    public List<SelectListItem> Suppliers { get; set; } = [];
}
