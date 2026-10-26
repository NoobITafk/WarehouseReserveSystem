using System.Text;
using WarehouseReserveSystem.Models;
using WarehouseReserveSystem.Services;
using Xunit;

namespace WarehouseReserveSystem.Tests;

public class ReportExportServiceTests
{
    [Fact]
    public void ExportStockBalancesToCsv_ProducesValidUtf8Csv()
    {
        // Arrange
        var service = new ReportExportService();
        var cat = new Category { Name = "Електроніка" };
        var p = new Product
        {
            Sku = "SKU-CSV-1",
            Name = "Мультиметр",
            Barcode = "1234567890123",
            Category = cat,
            CostPrice = 750m,
            Unit = "шт"
        };
        var wh = new Warehouse { Name = "Склад-1" };
        var zone = new StorageZone { Name = "Зона-А", Warehouse = wh };
        var loc = new StorageLocation { Code = "A-01-01", StorageZone = zone };

        var items = new List<StockItem>
        {
            new()
            {
                Product = p,
                StorageLocation = loc,
                Quantity = 12,
                BatchNumber = "BATCH-99"
            }
        };

        // Act
        var bytes = service.ExportStockBalancesToCsv(items);

        // Assert
        Assert.NotNull(bytes);
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Contains("Артикул (SKU)", text);
        Assert.Contains("SKU-CSV-1", text);
        Assert.Contains("Мультиметр", text);
        Assert.Contains("12", text);
    }

    [Fact]
    public void ExportMovementsToCsv_ProducesValidMovementRegistryCsv()
    {
        // Arrange
        var service = new ReportExportService();
        var p = new Product { Sku = "SKU-DOC", Name = "Осцилограф" };
        var sup = new Supplier { Name = "ТехноПостач" };

        var movements = new List<StockMovement>
        {
            new()
            {
                DocumentNumber = "DOC-REC-001",
                Type = StockMovementType.Receipt,
                Status = StockMovementStatus.Completed,
                Supplier = sup,
                StaffLogin = "admin",
                CreatedAt = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc),
                Items = new List<StockMovementItem>
                {
                    new()
                    {
                        Product = p,
                        Quantity = 5,
                        UnitPrice = 5000m
                    }
                }
            }
        };

        // Act
        var bytes = service.ExportMovementsToCsv(movements);

        // Assert
        Assert.NotNull(bytes);
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Contains("Номер документа", text);
        Assert.Contains("DOC-REC-001", text);
        Assert.Contains("Receipt", text);
        Assert.Contains("ТехноПостач", text);
    }
}
