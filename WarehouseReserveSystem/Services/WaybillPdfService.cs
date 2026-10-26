using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using WarehouseReserveSystem.Models;

namespace WarehouseReserveSystem.Services;

public class WaybillPdfService : IWaybillPdfService
{
    public byte[] GenerateWaybillPdf(StockMovement movement)
    {
        var document = new PdfDocument();
        document.Info.Title = $"Накладна {movement.DocumentNumber}";
        document.Info.Author = "WarehouseReserve WMS";

        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        var gfx = XGraphics.FromPdfPage(page);

        var titleFont = new XFont("Helvetica", 16, XFontStyle.Bold);
        var subTitleFont = new XFont("Helvetica", 11, XFontStyle.Regular);
        var boldFont = new XFont("Helvetica", 9, XFontStyle.Bold);
        var regularFont = new XFont("Helvetica", 9, XFontStyle.Regular);
        var smallFont = new XFont("Helvetica", 8, XFontStyle.Regular);

        var primaryBrush = new XSolidBrush(XColor.FromArgb(17, 24, 39));
        var mutedBrush = new XSolidBrush(XColor.FromArgb(107, 114, 128));
        var borderPen = new XPen(XColor.FromArgb(229, 231, 235), 0.75);
        var headerBg = new XSolidBrush(XColor.FromArgb(243, 244, 246));

        double margin = 40;
        double currentY = margin;
        double pageWidth = page.Width - 2 * margin;

        // 1. Header & Logo bar
        string docTitle = movement.Type switch
        {
            StockMovementType.Receipt => "ПРИБУТКОВА НАКЛАДНА",
            StockMovementType.Shipment => "ВИДАТКОВА НАКЛАДНА",
            StockMovementType.Transfer => "АКТ ВНУТРІШНЬОГО ПЕРЕМІЩЕННЯ",
            StockMovementType.WriteOff => "АКТ СПИСАННЯ МАТЕРІАЛЬНИХ ЦІННОСТЕЙ",
            _ => "СКЛАДСЬКИЙ ДОКУМЕНТ"
        };

        gfx.DrawString("WAREHOUSE RESERVE SYSTEM · WMS ENTERPRISE", smallFont, mutedBrush, margin, currentY);
        currentY += 16;
        gfx.DrawString($"{docTitle} № {movement.DocumentNumber}", titleFont, primaryBrush, margin, currentY);
        currentY += 16;
        gfx.DrawString($"Дата проведення: {movement.CreatedAt:dd.MM.yyyy HH:mm} (UTC) | Статус: {movement.Status}", subTitleFont, mutedBrush, margin, currentY);
        currentY += 24;

        gfx.DrawLine(borderPen, margin, currentY, margin + pageWidth, currentY);
        currentY += 16;

        // 2. Counterparty info
        if (movement.Type == StockMovementType.Receipt && movement.Supplier != null)
        {
            gfx.DrawString($"Постачальник: {movement.Supplier.Name} (ЄДРПОУ: {movement.Supplier.TaxNumber})", boldFont, primaryBrush, margin, currentY);
            currentY += 14;
            gfx.DrawString($"Адреса: {movement.Supplier.Address} | Контакт: {movement.Supplier.ContactPerson} ({movement.Supplier.Phone})", regularFont, mutedBrush, margin, currentY);
            currentY += 16;
        }
        else if (movement.Type == StockMovementType.Shipment)
        {
            gfx.DrawString($"Одержувач / Замовник: {movement.CustomerName ?? "Кінцевий покупець"}", boldFont, primaryBrush, margin, currentY);
            currentY += 14;
            gfx.DrawString($"Підстава відвантаження: {movement.Reason ?? "Замовлення покупця"}", regularFont, mutedBrush, margin, currentY);
            currentY += 16;
        }
        else if (movement.Type == StockMovementType.Transfer)
        {
            gfx.DrawString($"Підстава переміщення: {movement.Reason ?? "Внутрішня ротація складських залишків"}", boldFont, primaryBrush, margin, currentY);
            currentY += 16;
        }
        else if (movement.Type == StockMovementType.WriteOff)
        {
            gfx.DrawString($"Причина списання: {movement.Reason ?? "Брак / Пошкодження"}", boldFont, primaryBrush, margin, currentY);
            currentY += 16;
        }

        if (!string.IsNullOrWhiteSpace(movement.Notes))
        {
            gfx.DrawString($"Примітки: {movement.Notes}", smallFont, mutedBrush, margin, currentY);
            currentY += 16;
        }

        currentY += 8;

        // 3. Table Header
        double colNo = 25;
        double colSku = 80;
        double colName = 180;
        double colLoc = 70;
        double colQty = 45;
        double colPrice = 55;

        gfx.DrawRectangle(headerBg, margin, currentY, pageWidth, 22);
        gfx.DrawRectangle(borderPen, margin, currentY, pageWidth, 22);

        double x = margin + 4;
        gfx.DrawString("№", boldFont, primaryBrush, x, currentY + 15); x += colNo;
        gfx.DrawString("Артикул", boldFont, primaryBrush, x, currentY + 15); x += colSku;
        gfx.DrawString("Найменування товару", boldFont, primaryBrush, x, currentY + 15); x += colName;
        gfx.DrawString("Комірка", boldFont, primaryBrush, x, currentY + 15); x += colLoc;
        gfx.DrawString("К-сть", boldFont, primaryBrush, x, currentY + 15); x += colQty;
        gfx.DrawString("Ціна", boldFont, primaryBrush, x, currentY + 15); x += colPrice;
        gfx.DrawString("Сума, грн", boldFont, primaryBrush, x, currentY + 15);

        currentY += 22;

        // 4. Table Rows
        int index = 1;
        int totalUnits = 0;
        decimal totalAmount = 0m;

        foreach (var item in movement.Items)
        {
            decimal rowTotal = item.Quantity * item.UnitPrice;
            totalUnits += item.Quantity;
            totalAmount += rowTotal;

            string locCode = item.DestinationLocation?.Code 
                ?? item.SourceLocation?.Code 
                ?? "-";

            string productName = item.Product?.Name ?? "Товар";
            if (productName.Length > 36) productName = productName[..34] + "..";

            gfx.DrawRectangle(borderPen, margin, currentY, pageWidth, 20);

            x = margin + 4;
            gfx.DrawString(index.ToString(), regularFont, primaryBrush, x, currentY + 14); x += colNo;
            gfx.DrawString(item.Product?.Sku ?? "-", regularFont, primaryBrush, x, currentY + 14); x += colSku;
            gfx.DrawString(productName, regularFont, primaryBrush, x, currentY + 14); x += colName;
            gfx.DrawString(locCode, regularFont, primaryBrush, x, currentY + 14); x += colLoc;
            gfx.DrawString($"{item.Quantity} {item.Product?.Unit ?? "шт"}", regularFont, primaryBrush, x, currentY + 14); x += colQty;
            gfx.DrawString($"{item.UnitPrice:N2}", regularFont, primaryBrush, x, currentY + 14); x += colPrice;
            gfx.DrawString($"{rowTotal:N2}", boldFont, primaryBrush, x, currentY + 14);

            currentY += 20;
            index++;

            // Simple pagination if too many items
            if (currentY > page.Height - 120)
            {
                page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                currentY = margin;
            }
        }

        // 5. Total Row
        gfx.DrawRectangle(headerBg, margin, currentY, pageWidth, 24);
        gfx.DrawRectangle(borderPen, margin, currentY, pageWidth, 24);
        gfx.DrawString("РАЗОМ ДО СПЛАТИ / ПРОВЕДЕННЯ:", boldFont, primaryBrush, margin + 8, currentY + 16);
        gfx.DrawString($"{totalUnits} од.", boldFont, primaryBrush, margin + colNo + colSku + colName + colLoc, currentY + 16);
        gfx.DrawString($"{totalAmount:N2} грн", titleFont, primaryBrush, margin + pageWidth - 120, currentY + 17);

        currentY += 50;

        // 6. Signatures
        gfx.DrawString("Відповідальний за оформлення: ___________________ / " + movement.StaffLogin + " /", regularFont, primaryBrush, margin, currentY);
        currentY += 25;
        gfx.DrawString("Товар отримав (Одержувач):      ___________________ / М.П. /", regularFont, primaryBrush, margin, currentY);

        using var memoryStream = new MemoryStream();
        document.Save(memoryStream, false);
        return memoryStream.ToArray();
    }
}
