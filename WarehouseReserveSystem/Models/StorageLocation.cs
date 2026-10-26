namespace WarehouseReserveSystem.Models;

public class StorageLocation
{
    public int Id { get; set; }
    public int StorageZoneId { get; set; }
    
    // Physical coordinate: Zone-Aisle-Rack-Shelf-Bin, e.g. "A-01-02-B"
    public string Code { get; set; } = string.Empty;
    public string Aisle { get; set; } = string.Empty;   // Ряд
    public string Rack { get; set; } = string.Empty;    // Стелаж
    public string Shelf { get; set; } = string.Empty;   // Полиця
    public string Bin { get; set; } = string.Empty;     // Комірка

    public decimal MaxWeightKg { get; set; } = 1000m;
    public decimal MaxVolumeM3 { get; set; } = 2.5m;
    public bool IsActive { get; set; } = true;

    public StorageZone? StorageZone { get; set; }
    public ICollection<StockItem> StockItems { get; set; } = [];
}
