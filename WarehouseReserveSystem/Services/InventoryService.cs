using Microsoft.EntityFrameworkCore;
using WarehouseReserveSystem.Data;
using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.Services;

public class InventoryService(ApplicationDbContext context, IAuditService auditService) : IInventoryService
{
    public async Task<IReadOnlyList<StockItem>> GetStockBalancesAsync(int? warehouseId = null, int? categoryId = null, string? search = null)
    {
        var query = context.StockItems
            .Include(s => s.Product!)
                .ThenInclude(p => p.Category)
            .Include(s => s.StorageLocation!)
                .ThenInclude(l => l.StorageZone!)
                    .ThenInclude(z => z.Warehouse)
            .AsNoTracking();

        if (warehouseId.HasValue)
        {
            query = query.Where(s => s.StorageLocation!.StorageZone!.WarehouseId == warehouseId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(s => s.Product!.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => 
                s.Product!.Name.ToLower().Contains(term) ||
                s.Product!.Sku.ToLower().Contains(term) ||
                s.Product!.Barcode.ToLower().Contains(term) ||
                s.StorageLocation!.Code.ToLower().Contains(term));
        }

        return await query
            .OrderBy(s => s.Product!.Name)
            .ThenBy(s => s.StorageLocation!.Code)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProductStockSummary>> GetLowStockAlertsAsync()
    {
        var products = await context.Products
            .Include(p => p.Category)
            .Include(p => p.StockItems)
            .AsNoTracking()
            .ToListAsync();

        var summaries = products
            .Select(p =>
            {
                var currentStock = p.StockItems.Sum(s => s.Quantity);
                return new ProductStockSummary(
                    p.Id,
                    p.Sku,
                    p.Name,
                    p.Category?.Name ?? "Без категорії",
                    p.Unit,
                    currentStock,
                    p.MinStockThreshold,
                    p.CostPrice,
                    currentStock <= p.MinStockThreshold
                );
            })
            .Where(s => s.IsCritical)
            .OrderBy(s => s.CurrentStock)
            .ToList();

        return summaries;
    }

    public async Task<decimal> GetTotalValuationAsync()
    {
        var items = await context.StockItems
            .AsNoTracking()
            .Select(s => new { s.Quantity, CostPrice = s.Product != null ? s.Product.CostPrice : 0m })
            .ToListAsync();

        return items.Sum(s => s.Quantity * s.CostPrice);
    }

    public async Task<int> GetTotalUnitsCountAsync()
    {
        return await context.StockItems.SumAsync(s => s.Quantity);
    }

    public async Task<int> GetTotalActiveSkuCountAsync()
    {
        return await context.Products.CountAsync(p => p.IsActive);
    }

    public async Task<StockMovement> ExecuteReceiptAsync(
        int supplierId, 
        int destinationLocationId, 
        List<(int productId, int quantity, decimal unitPrice, string? batchNumber)> items, 
        string? notes, 
        string staffLogin)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var destLocation = await context.StorageLocations
                .Include(l => l.StorageZone)
                .FirstOrDefaultAsync(l => l.Id == destinationLocationId)
                ?? throw new InvalidOperationException($"Комірку призначення з ID {destinationLocationId} не знайдено.");

            var count = await context.StockMovements.CountAsync();
            var docNumber = $"DOC-REC-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):0000}";

            var movement = new StockMovement
            {
                DocumentNumber = docNumber,
                Type = StockMovementType.Receipt,
                Status = StockMovementStatus.Completed,
                SupplierId = supplierId,
                StaffLogin = staffLogin,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            decimal totalSum = 0m;

            foreach (var item in items)
            {
                if (item.quantity <= 0)
                    throw new ArgumentException("Кількість товару повинна бути більшою за нуль.");

                var product = await context.Products.FindAsync(item.productId)
                    ?? throw new InvalidOperationException($"Товар з ID {item.productId} не знайдено.");

                totalSum += item.quantity * item.unitPrice;

                movement.Items.Add(new StockMovementItem
                {
                    ProductId = item.productId,
                    DestinationLocationId = destinationLocationId,
                    Quantity = item.quantity,
                    UnitPrice = item.unitPrice,
                    BatchNumber = item.batchNumber
                });

                // Update or create StockItem in destination location
                var stockItem = await context.StockItems
                    .FirstOrDefaultAsync(s => s.ProductId == item.productId && s.StorageLocationId == destinationLocationId);

                if (stockItem is null)
                {
                    stockItem = new StockItem
                    {
                        ProductId = item.productId,
                        StorageLocationId = destinationLocationId,
                        Quantity = item.quantity,
                        BatchNumber = item.batchNumber,
                        LastUpdatedAt = DateTime.UtcNow
                    };
                    context.StockItems.Add(stockItem);
                }
                else
                {
                    stockItem.Quantity += item.quantity;
                    stockItem.LastUpdatedAt = DateTime.UtcNow;
                }
            }

            context.StockMovements.Add(movement);
            await context.SaveChangesAsync();

            await auditService.LogAsync(
                "StockReceipt",
                "StockMovement",
                movement.DocumentNumber,
                $"Оприбутковано {items.Count} позицій у комірку {destLocation.Code} на суму {totalSum:N2} грн",
                staffLogin
            );

            await transaction.CommitAsync();
            return movement;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<StockMovement> ExecuteShipmentAsync(
        string customerName, 
        int sourceLocationId, 
        List<(int productId, int quantity, decimal unitPrice)> items, 
        string? notes, 
        string staffLogin)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var sourceLoc = await context.StorageLocations
                .FirstOrDefaultAsync(l => l.Id == sourceLocationId)
                ?? throw new InvalidOperationException($"Вихідну комірку з ID {sourceLocationId} не знайдено.");

            var count = await context.StockMovements.CountAsync();
            var docNumber = $"DOC-SHP-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):0000}";

            var movement = new StockMovement
            {
                DocumentNumber = docNumber,
                Type = StockMovementType.Shipment,
                Status = StockMovementStatus.Completed,
                CustomerName = customerName,
                StaffLogin = staffLogin,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            decimal totalSum = 0m;

            foreach (var item in items)
            {
                if (item.quantity <= 0)
                    throw new ArgumentException("Кількість для відвантаження повинна бути більшою за нуль.");

                var stockItem = await context.StockItems
                    .Include(s => s.Product)
                    .FirstOrDefaultAsync(s => s.ProductId == item.productId && s.StorageLocationId == sourceLocationId)
                    ?? throw new InvalidOperationException($"Товар не знайдено у вказаній комірці {sourceLoc.Code}.");

                if (stockItem.Quantity < item.quantity)
                {
                    throw new InvalidOperationException(
                        $"Недостатньо залишку товару '{stockItem.Product?.Name}'. На складі: {stockItem.Quantity}, запитано: {item.quantity}");
                }

                stockItem.Quantity -= item.quantity;
                stockItem.LastUpdatedAt = DateTime.UtcNow;

                totalSum += item.quantity * item.unitPrice;

                movement.Items.Add(new StockMovementItem
                {
                    ProductId = item.productId,
                    SourceLocationId = sourceLocationId,
                    Quantity = item.quantity,
                    UnitPrice = item.unitPrice
                });
            }

            context.StockMovements.Add(movement);
            await context.SaveChangesAsync();

            await auditService.LogAsync(
                "StockShipment",
                "StockMovement",
                movement.DocumentNumber,
                $"Відвантажено для '{customerName}' з комірки {sourceLoc.Code} на суму {totalSum:N2} грн",
                staffLogin
            );

            await transaction.CommitAsync();
            return movement;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<StockMovement> ExecuteTransferAsync(
        int sourceLocationId, 
        int destinationLocationId, 
        int productId, 
        int quantity, 
        string? notes, 
        string staffLogin)
    {
        if (sourceLocationId == destinationLocationId)
            throw new ArgumentException("Вихідна та цільова комірки не можуть бути однаковими.");

        if (quantity <= 0)
            throw new ArgumentException("Кількість для переміщення повинна бути більшою за нуль.");

        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var sourceLoc = await context.StorageLocations.FindAsync(sourceLocationId)
                ?? throw new InvalidOperationException("Вихідну комірку не знайдено.");
            var destLoc = await context.StorageLocations.FindAsync(destinationLocationId)
                ?? throw new InvalidOperationException("Цільову комірку не знайдено.");

            var sourceStock = await context.StockItems
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.StorageLocationId == sourceLocationId)
                ?? throw new InvalidOperationException($"Товар відсутній у вихідній комірці {sourceLoc.Code}.");

            if (sourceStock.Quantity < quantity)
            {
                throw new InvalidOperationException(
                    $"Недостатня кількість для переміщення. Доступно: {sourceStock.Quantity}, потрібно: {quantity}");
            }

            sourceStock.Quantity -= quantity;
            sourceStock.LastUpdatedAt = DateTime.UtcNow;

            var destStock = await context.StockItems
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.StorageLocationId == destinationLocationId);

            if (destStock is null)
            {
                destStock = new StockItem
                {
                    ProductId = productId,
                    StorageLocationId = destinationLocationId,
                    Quantity = quantity,
                    BatchNumber = sourceStock.BatchNumber,
                    LastUpdatedAt = DateTime.UtcNow
                };
                context.StockItems.Add(destStock);
            }
            else
            {
                destStock.Quantity += quantity;
                destStock.LastUpdatedAt = DateTime.UtcNow;
            }

            var count = await context.StockMovements.CountAsync();
            var docNumber = $"DOC-TRF-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):0000}";

            var movement = new StockMovement
            {
                DocumentNumber = docNumber,
                Type = StockMovementType.Transfer,
                Status = StockMovementStatus.Completed,
                StaffLogin = staffLogin,
                Notes = notes,
                Reason = $"Внутрішнє переміщення з {sourceLoc.Code} до {destLoc.Code}",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            movement.Items.Add(new StockMovementItem
            {
                ProductId = productId,
                SourceLocationId = sourceLocationId,
                DestinationLocationId = destinationLocationId,
                Quantity = quantity,
                UnitPrice = sourceStock.Product?.CostPrice ?? 0m,
                BatchNumber = sourceStock.BatchNumber
            });

            context.StockMovements.Add(movement);
            await context.SaveChangesAsync();

            await auditService.LogAsync(
                "StockTransfer",
                "StockMovement",
                movement.DocumentNumber,
                $"Переміщено {quantity} шт товару '{sourceStock.Product?.Name}' з {sourceLoc.Code} до {destLoc.Code}",
                staffLogin
            );

            await transaction.CommitAsync();
            return movement;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<StockMovement> ExecuteWriteOffAsync(
        int sourceLocationId, 
        int productId, 
        int quantity, 
        string reason, 
        string staffLogin)
    {
        if (quantity <= 0)
            throw new ArgumentException("Кількість для списання повинна бути більшою за нуль.");

        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var sourceLoc = await context.StorageLocations.FindAsync(sourceLocationId)
                ?? throw new InvalidOperationException("Комірку не знайдено.");

            var stock = await context.StockItems
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.StorageLocationId == sourceLocationId)
                ?? throw new InvalidOperationException("Товар відсутній у вказаній комірці.");

            if (stock.Quantity < quantity)
            {
                throw new InvalidOperationException($"Недостатньо залишку для списання. Доступно: {stock.Quantity}, запитано: {quantity}");
            }

            stock.Quantity -= quantity;
            stock.LastUpdatedAt = DateTime.UtcNow;

            var count = await context.StockMovements.CountAsync();
            var docNumber = $"DOC-WRO-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):0000}";

            var movement = new StockMovement
            {
                DocumentNumber = docNumber,
                Type = StockMovementType.WriteOff,
                Status = StockMovementStatus.Completed,
                StaffLogin = staffLogin,
                Reason = reason,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            movement.Items.Add(new StockMovementItem
            {
                ProductId = productId,
                SourceLocationId = sourceLocationId,
                Quantity = quantity,
                UnitPrice = stock.Product?.CostPrice ?? 0m
            });

            context.StockMovements.Add(movement);
            await context.SaveChangesAsync();

            await auditService.LogAsync(
                "StockWriteOff",
                "StockMovement",
                movement.DocumentNumber,
                $"Списано {quantity} шт товару '{stock.Product?.Name}' з комірки {sourceLoc.Code}. Причина: {reason}",
                staffLogin
            );

            await transaction.CommitAsync();
            return movement;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<StockMovement>> GetRecentMovementsAsync(int count = 10)
    {
        return await context.StockMovements
            .Include(m => m.Supplier)
            .Include(m => m.Items)
                .ThenInclude(i => i.Product)
            .Include(m => m.Items)
                .ThenInclude(i => i.SourceLocation)
            .Include(m => m.Items)
                .ThenInclude(i => i.DestinationLocation)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<StockMovement?> GetMovementByIdAsync(int id)
    {
        return await context.StockMovements
            .Include(m => m.Supplier)
            .Include(m => m.StaffUser)
            .Include(m => m.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Category)
            .Include(m => m.Items)
                .ThenInclude(i => i.SourceLocation)
            .Include(m => m.Items)
                .ThenInclude(i => i.DestinationLocation)
            .FirstOrDefaultAsync(m => m.Id == id);
    }
}
