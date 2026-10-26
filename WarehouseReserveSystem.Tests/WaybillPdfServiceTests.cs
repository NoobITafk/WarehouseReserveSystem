using WarehouseReserveSystem.Models;
using WarehouseReserveSystem.Services;
using Xunit;

namespace WarehouseReserveSystem.Tests;

public class WaybillPdfServiceTests
{
    [Fact]
    public void GenerateWaybillPdf_ProducesValidPdfBytes()
    {
        // Arrange
        var service = new WaybillPdfService();

        var movement = new StockMovement
        {
            DocumentNumber = "DOC-REC-20261026-0001",
            Type = StockMovementType.Receipt,
            Status = StockMovementStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            StaffLogin = "admin",
            Supplier = new Supplier
            {
                Name = "ТОВ «ПромЕлектроПостач»",
                TaxNumber = "38472910",
                Address = "м. Київ, вул. Промислова, 14",
                ContactPerson = "Віктор",
                Phone = "+380442345678"
            },
            Items =
            [
                new StockMovementItem
                {
                    ProductId = 1,
                    Quantity = 10,
                    UnitPrice = 14500m,
                    Product = new Product
                    {
                        Sku = "SKU-PLC-1200",
                        Name = "Контролер Siemens S7-1200",
                        Unit = "шт"
                    },
                    DestinationLocation = new StorageLocation
                    {
                        Code = "B-01-01-1"
                    }
                }
            ]
        };

        // Act
        var pdfBytes = service.GenerateWaybillPdf(movement);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 500, "PDF should not be empty");

        // Verify PDF Header (%PDF-)
        var header = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }
}
