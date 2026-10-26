using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.Services;

public interface IWarehouseService
{
    Task<IReadOnlyList<Warehouse>> GetAllWarehousesAsync();
    Task<Warehouse?> GetWarehouseByIdAsync(int id);
    Task<IReadOnlyList<StorageZone>> GetZonesByWarehouseIdAsync(int warehouseId);
    Task<IReadOnlyList<StorageLocation>> GetLocationsByZoneIdAsync(int zoneId);
    Task<IReadOnlyList<StorageLocation>> GetAllLocationsAsync();
    Task<StorageLocation?> GetLocationByIdAsync(int id);
}
