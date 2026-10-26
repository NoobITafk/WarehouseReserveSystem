using System.Text;
using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.Services;

public interface IReportExportService
{
    byte[] ExportStockBalancesToCsv(IReadOnlyList<StockItem> items);
    byte[] ExportMovementsToCsv(IReadOnlyList<StockMovement> movements);
}

public class ReportExportService : IReportExportService
{
    public byte[] ExportStockBalancesToCsv(IReadOnlyList<StockItem> items)
    {
        var sb = new StringBuilder();
        // UTF-8 BOM for Excel compatibility with Ukrainian characters
        sb.Append('\uFEFF');

        sb.AppendLine("Склад;Зона;Комірка;Артикул (SKU);Назва товару;Штрих-код;Категорія;Кількість;Од.вим.;Собівартість, грн;Сума залишку, грн;Партія;Оновлено");

        foreach (var item in items)
        {
            var p = item.Product;
            var loc = item.StorageLocation;
            var zone = loc?.StorageZone;
            var wh = zone?.Warehouse;

            decimal totalCost = item.Quantity * (p?.CostPrice ?? 0m);

            sb.AppendLine(
                $"\"{wh?.Name ?? "-"}\";" +
                $"\"{zone?.Name ?? "-"}\";" +
                $"\"{loc?.Code ?? "-"}\";" +
                $"\"{p?.Sku ?? "-"}\";" +
                $"\"{p?.Name ?? "-"}\";" +
                $"\"{p?.Barcode ?? "-"}\";" +
                $"\"{p?.Category?.Name ?? "-"}\";" +
                $"{item.Quantity};" +
                $"\"{p?.Unit ?? "шт"}\";" +
                $"{(p?.CostPrice ?? 0m):F2};" +
                $"{totalCost:F2};" +
                $"\"{item.BatchNumber ?? "-"}\";" +
                $"{item.LastUpdatedAt:dd.MM.yyyy HH:mm}"
            );
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] ExportMovementsToCsv(IReadOnlyList<StockMovement> movements)
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF');

        sb.AppendLine("Номер документа;Дата;Тип операції;Статус;Контрагент;Користувач;Кількість позицій;Сума, грн;Примітки");

        foreach (var m in movements)
        {
            var counterparty = m.Supplier?.Name ?? m.CustomerName ?? "-";
            int totalQty = m.Items.Sum(i => i.Quantity);
            decimal totalSum = m.Items.Sum(i => i.Quantity * i.UnitPrice);

            sb.AppendLine(
                $"\"{m.DocumentNumber}\";" +
                $"{m.CreatedAt:dd.MM.yyyy HH:mm};" +
                $"\"{m.Type}\";" +
                $"\"{m.Status}\";" +
                $"\"{counterparty}\";" +
                $"\"{m.StaffLogin}\";" +
                $"{totalQty};" +
                $"{totalSum:F2};" +
                $"\"{m.Notes ?? m.Reason ?? "-"}\""
            );
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
