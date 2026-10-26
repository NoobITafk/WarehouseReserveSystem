using Microsoft.EntityFrameworkCore;
using WarehouseReserveSystem.Data;
using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.Services;

public class WarehouseService(ApplicationDbContext context) : IWarehouseService
{
    public async Task<IReadOnlyList<Warehouse>> GetAllWarehousesAsync()
    {
        return await context.Warehouses
            .Include(w => w.Zones)
                .ThenInclude(z => z.Locations)
            .OrderBy(w => w.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Warehouse?> GetWarehouseByIdAsync(int id)
    {
        return await context.Warehouses
            .Include(w => w.Zones)
                .ThenInclude(z => z.Locations)
                    .ThenInclude(l => l.StockItems)
                        .ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<IReadOnlyList<StorageZone>> GetZonesByWarehouseIdAsync(int warehouseId)
    {
        return await context.StorageZones
            .Where(z => z.WarehouseId == warehouseId)
            .Include(z => z.Locations)
            .OrderBy(z => z.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<StorageLocation>> GetLocationsByZoneIdAsync(int zoneId)
    {
        return await context.StorageLocations
            .Where(l => l.StorageZoneId == zoneId)
            .Include(l => l.StockItems)
                .ThenInclude(s => s.Product)
            .OrderBy(l => l.Code)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<StorageLocation>> GetAllLocationsAsync()
    {
        return await context.StorageLocations
            .Include(l => l.StorageZone!)
                .ThenInclude(z => z.Warehouse)
            .OrderBy(l => l.Code)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<StorageLocation?> GetLocationByIdAsync(int id)
    {
        return await context.StorageLocations
            .Include(l => l.StorageZone!)
                .ThenInclude(z => z.Warehouse)
            .Include(l => l.StockItems)
                .ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(l => l.Id == id);
    }
}
