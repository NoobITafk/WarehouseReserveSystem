namespace WarehouseReserveSystem.Models;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty; // Артикул, e.g. "SKU-EL-1042"
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Barcode { get; set; } = string.Empty; // EAN-13, e.g. "4820000000012"
    
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }
    
    public string Unit { get; set; } = "шт"; // шт, кг, упак, л, м
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    
    // Critical low stock threshold
    public int MinStockThreshold { get; set; } = 10;
    public decimal WeightKg { get; set; } = 0.5m;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Category? Category { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<StockItem> StockItems { get; set; } = [];
}
