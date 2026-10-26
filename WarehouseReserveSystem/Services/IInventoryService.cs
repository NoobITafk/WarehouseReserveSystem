using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.Services;

public interface IInventoryService
{
    Task<IReadOnlyList<StockItem>> GetStockBalancesAsync(int? warehouseId = null, int? categoryId = null, string? search = null);
    Task<IReadOnlyList<ProductStockSummary>> GetLowStockAlertsAsync();
    Task<decimal> GetTotalValuationAsync();
    Task<int> GetTotalUnitsCountAsync();
    Task<int> GetTotalActiveSkuCountAsync();

    Task<StockMovement> ExecuteReceiptAsync(
        int supplierId, 
        int destinationLocationId, 
        List<(int productId, int quantity, decimal unitPrice, string? batchNumber)> items, 
        string? notes, 
        string staffLogin);

    Task<StockMovement> ExecuteShipmentAsync(
        string customerName, 
        int sourceLocationId, 
        List<(int productId, int quantity, decimal unitPrice)> items, 
        string? notes, 
        string staffLogin);

    Task<StockMovement> ExecuteTransferAsync(
        int sourceLocationId, 
        int destinationLocationId, 
        int productId, 
        int quantity, 
        string? notes, 
        string staffLogin);

    Task<StockMovement> ExecuteWriteOffAsync(
        int sourceLocationId, 
        int productId, 
        int quantity, 
        string reason, 
        string staffLogin);

    Task<IReadOnlyList<StockMovement>> GetRecentMovementsAsync(int count = 10);
    Task<StockMovement?> GetMovementByIdAsync(int id);
}

public record ProductStockSummary(
    int ProductId, 
    string Sku, 
    string Name, 
    string CategoryName, 
    string Unit, 
    int CurrentStock, 
    int MinThreshold, 
    decimal CostPrice,
    bool IsCritical);
