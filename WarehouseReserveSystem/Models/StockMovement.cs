namespace WarehouseReserveSystem.Models;

public enum StockMovementType
{
    Receipt = 1,    // Оприбуткування (від постачальника на склад)
    Shipment = 2,   // Відвантаження (зі складу покупцю/замовнику)
    Transfer = 3,   // Внутрішнє переміщення (з комірки A в комірку B)
    WriteOff = 4    // Списання (брак, пошкодження, сплив терміну)
}

public enum StockMovementStatus
{
    Draft = 1,
    Completed = 2,
    Cancelled = 3
}

public class StockMovement
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty; // e.g. "DOC-2026-0001"
    public StockMovementType Type { get; set; }
    public StockMovementStatus Status { get; set; } = StockMovementStatus.Completed;

    public int? SupplierId { get; set; }
    public string? CustomerName { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }

    public int? StaffUserId { get; set; }
    public string StaffLogin { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Supplier? Supplier { get; set; }
    public WarehouseStaff? StaffUser { get; set; }
    public ICollection<StockMovementItem> Items { get; set; } = [];
}
