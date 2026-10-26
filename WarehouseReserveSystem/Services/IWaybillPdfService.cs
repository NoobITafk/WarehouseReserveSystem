using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.Services;

public interface IWaybillPdfService
{
    byte[] GenerateWaybillPdf(StockMovement movement);
}
