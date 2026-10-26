namespace WarehouseReserveSystem.Models;

public class StockItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int StorageLocationId { get; set; }
    
    public int Quantity { get; set; }
    public string? BatchNumber { get; set; } // Серія / партія
    public DateTime? ExpiryDate { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public Product? Product { get; set; }
    public StorageLocation? StorageLocation { get; set; }
}
