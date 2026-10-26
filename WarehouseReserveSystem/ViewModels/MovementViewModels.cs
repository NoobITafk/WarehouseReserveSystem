using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WarehouseReserveSystem.ViewModels;

public class ReceiptFormViewModel
{
    [Required(ErrorMessage = "Оберіть постачальника.")]
    [Display(Name = "Постачальник")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Оберіть комірку розміщення.")]
    [Display(Name = "Адресна комірка складу")]
    public int DestinationLocationId { get; set; }

    [Required(ErrorMessage = "Оберіть товар.")]
    [Display(Name = "Товар")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Вкажіть кількість.")]
    [Range(1, 1000000, ErrorMessage = "Кількість повинна бути більшою за 0.")]
    [Display(Name = "Кількість")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Вкажіть ціну закупівлі.")]
    [Range(0.01, 10000000, ErrorMessage = "Ціна повинна бути більшою за 0.")]
    [Display(Name = "Ціна закупівлі за од. (грн)")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Номер партії / Серія")]
    public string? BatchNumber { get; set; }

    [Display(Name = "Примітки / Номер ТТН постачальника")]
    public string? Notes { get; set; }

    public List<SelectListItem> Suppliers { get; set; } = [];
    public List<SelectListItem> Locations { get; set; } = [];
    public List<SelectListItem> Products { get; set; } = [];
}

public class ShipmentFormViewModel
{
    [Required(ErrorMessage = "Вкажіть замовника / клієнта.")]
    [Display(Name = "Замовник / Одержувач")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Оберіть комірку списання.")]
    [Display(Name = "З якої комірки відвантажити")]
    public int SourceLocationId { get; set; }

    [Required(ErrorMessage = "Оберіть товар.")]
    [Display(Name = "Товар")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Вкажіть кількість.")]
    [Range(1, 1000000, ErrorMessage = "Кількість повинна бути більшою за 0.")]
    [Display(Name = "Кількість")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Вкажіть ціну реалізації.")]
    [Range(0.01, 10000000, ErrorMessage = "Ціна повинна бути більшою за 0.")]
    [Display(Name = "Ціна реалізації за од. (грн)")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Примітки / Номер замовлення")]
    public string? Notes { get; set; }

    public List<SelectListItem> Locations { get; set; } = [];
    public List<SelectListItem> Products { get; set; } = [];
}

public class TransferFormViewModel
{
    [Required(ErrorMessage = "Оберіть товар для переміщення.")]
    [Display(Name = "Товар")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Оберіть вихідну комірку.")]
    [Display(Name = "З комірки (Звідки)")]
    public int SourceLocationId { get; set; }

    [Required(ErrorMessage = "Оберіть цільову комірку.")]
    [Display(Name = "У комірку (Куди)")]
    public int DestinationLocationId { get; set; }

    [Required(ErrorMessage = "Вкажіть кількість.")]
    [Range(1, 1000000, ErrorMessage = "Кількість повинна бути більшою за 0.")]
    [Display(Name = "Кількість")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Підстава / Примітки")]
    public string? Notes { get; set; }

    public List<SelectListItem> Products { get; set; } = [];
    public List<SelectListItem> Locations { get; set; } = [];
}
