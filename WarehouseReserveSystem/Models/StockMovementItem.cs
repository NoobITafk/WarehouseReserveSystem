namespace WarehouseReserveSystem.Models;

public class StockMovementItem
{
    public int Id { get; set; }
    public int StockMovementId { get; set; }
    public int ProductId { get; set; }

    // Physical storage addresses:
    public int? SourceLocationId { get; set; }      // From where (for Transfer, Shipment, WriteOff)
    public int? DestinationLocationId { get; set; } // To where (for Receipt, Transfer)

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? BatchNumber { get; set; }

    public StockMovement? StockMovement { get; set; }
    public Product? Product { get; set; }
    public StorageLocation? SourceLocation { get; set; }
    public StorageLocation? DestinationLocation { get; set; }
}
