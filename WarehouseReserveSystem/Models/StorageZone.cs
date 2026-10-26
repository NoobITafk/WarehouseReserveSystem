namespace WarehouseReserveSystem.Models;

public class StorageZone
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // e.g. ZONE-A
    public string? Description { get; set; }
    public string ZoneType { get; set; } = "Standard"; // Standard, ColdStorage, Hazardous, HighRack

    public Warehouse? Warehouse { get; set; }
    public ICollection<StorageLocation> Locations { get; set; } = [];
}
