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
        document.Info.Author = "WarehouseReserve WMS Enterprise";
        document.Info.Subject = "Офіційний первинний документ бухгалтерського та складського обліку";

        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        var gfx = XGraphics.FromPdfPage(page);

        // Fonts
        var headerFont = new XFont("Helvetica", 14, XFontStyle.Bold);
        var subHeaderFont = new XFont("Helvetica", 10, XFontStyle.Bold);
        var boldFont = new XFont("Helvetica", 8, XFontStyle.Bold);
        var regularFont = new XFont("Helvetica", 8, XFontStyle.Regular);
        var smallFont = new XFont("Helvetica", 7, XFontStyle.Regular);
        var italicFont = new XFont("Helvetica", 7.5, XFontStyle.Italic);

        // Brushes and Pens
        var primaryBrush = new XSolidBrush(XColor.FromArgb(15, 23, 42));       // Slate 900
        var secondaryBrush = new XSolidBrush(XColor.FromArgb(71, 85, 105));    // Slate 600
        var mutedBrush = new XSolidBrush(XColor.FromArgb(148, 163, 184));      // Slate 400
        var darkPen = new XPen(XColor.FromArgb(15, 23, 42), 1.0);
        var borderPen = new XPen(XColor.FromArgb(203, 213, 225), 0.6);        // Slate 300
        var tableHeaderBg = new XSolidBrush(XColor.FromArgb(241, 245, 249));   // Slate 100
        var zebraBg = new XSolidBrush(XColor.FromArgb(248, 250, 252));          // Slate 50

        double margin = 32;
        double currentY = margin;
        double pageWidth = page.Width - 2 * margin;

        // 1. Legal Company Requisites (Header)
        string docTitle = movement.Type switch
        {
            StockMovementType.Receipt => "ПРИБУТКОВА НАКЛАДНА",
            StockMovementType.Shipment => "ВИДАТКОВА НАКЛАДНА",
            StockMovementType.Transfer => "АКТ ВНУТРІШНЬОГО СКЛАДСЬКОГО ПЕРЕМІЩЕННЯ",
            StockMovementType.WriteOff => "АКТ СПИСАННЯ МАТЕРІАЛЬНИХ ЦІННОСТЕЙ",
            _ => "НАКЛАДНА НА ВІДПУСК (ПРИЙМАННЯ)"
        };

        // Supplier Box
        gfx.DrawString("ПОСТАЧАЛЬНИК:", boldFont, secondaryBrush, margin, currentY);
        gfx.DrawString("ТОВ «ВЕРЕС-ЛОГІСТИК»  |  Код ЄДРПОУ: 39482105  |  ІПН: 394821026591", boldFont, primaryBrush, margin + 85, currentY);
        currentY += 12;
        gfx.DrawString("Розрахунковий рахунок:", smallFont, mutedBrush, margin, currentY);
        gfx.DrawString("UA423052990000026001234567890 в АТ КБ «ПРИВАТБАНК», МФО 305299", regularFont, primaryBrush, margin + 85, currentY);
        currentY += 12;
        gfx.DrawString("Адреса та контакти:", smallFont, mutedBrush, margin, currentY);
        gfx.DrawString("03056, м. Київ, вул. Польова, буд. 24, тел. +380 (44) 230-01-02, e-mail: logistics@veres.ua", regularFont, primaryBrush, margin + 85, currentY);
        currentY += 16;

        gfx.DrawLine(borderPen, margin, currentY, margin + pageWidth, currentY);
        currentY += 12;

        // Customer / Consignee Box
        string customerName = movement.CustomerName 
            ?? movement.Supplier?.Name 
            ?? "ТОВ «ПРОМЕНЕРГО-АВТОМАТИКА» (ЄДРПОУ 41295830)";
        
        string contractRef = movement.Notes?.Contains("Договір") == true 
            ? movement.Notes 
            : "Договір поставки матеріально-технічних цінностей № 14/02 від 15.01.2026 р.";

        gfx.DrawString("ОДЕРЖУВАЧ:", boldFont, secondaryBrush, margin, currentY);
        gfx.DrawString(customerName, boldFont, primaryBrush, margin + 85, currentY);
        currentY += 12;
        gfx.DrawString("Підстава (Договір):", smallFont, mutedBrush, margin, currentY);
        gfx.DrawString(contractRef, regularFont, primaryBrush, margin + 85, currentY);
        currentY += 12;
        gfx.DrawString("Склад відвантаження:", smallFont, mutedBrush, margin, currentY);
        gfx.DrawString("Центральний розподільчий комплекс «Хаб Північ» (Склад №1, м. Київ)", regularFont, primaryBrush, margin + 85, currentY);
        currentY += 18;

        // 2. Title Banner
        gfx.DrawLine(darkPen, margin, currentY, margin + pageWidth, currentY);
        currentY += 14;
        gfx.DrawString($"{docTitle} № {movement.DocumentNumber}", headerFont, primaryBrush, margin, currentY);
        currentY += 14;
        gfx.DrawString($"від {movement.CreatedAt:dd.MM.yyyy} р.  (проведено в системі о {movement.CreatedAt:HH:mm} UTC, оператор: {movement.StaffLogin})", regularFont, secondaryBrush, margin, currentY);
        currentY += 14;
        gfx.DrawLine(darkPen, margin, currentY, margin + pageWidth, currentY);
        currentY += 12;

        // 3. Specification Table Header
        double colNo = 22;
        double colSku = 68;
        double colUkt = 65;
        double colName = 175;
        double colUnit = 32;
        double colQty = 40;
        double colPrice = 58;
        double colSum = pageWidth - (colNo + colSku + colUkt + colName + colUnit + colQty + colPrice);

        gfx.DrawRectangle(tableHeaderBg, margin, currentY, pageWidth, 22);
        gfx.DrawRectangle(borderPen, margin, currentY, pageWidth, 22);

        double x = margin + 3;
        gfx.DrawString("№", boldFont, primaryBrush, x, currentY + 15); x += colNo;
        gfx.DrawString("Артикул", boldFont, primaryBrush, x, currentY + 15); x += colSku;
        gfx.DrawString("УКТ ЗЕД", boldFont, primaryBrush, x, currentY + 15); x += colUkt;
        gfx.DrawString("Найменування ТМЦ", boldFont, primaryBrush, x, currentY + 15); x += colName;
        gfx.DrawString("Од.", boldFont, primaryBrush, x, currentY + 15); x += colUnit;
        gfx.DrawString("К-сть", boldFont, primaryBrush, x, currentY + 15); x += colQty;
        gfx.DrawString("Ціна без ПДВ", boldFont, primaryBrush, x, currentY + 15); x += colPrice;
        gfx.DrawString("Сума без ПДВ", boldFont, primaryBrush, x, currentY + 15);

        currentY += 22;

        // 4. Specification Rows
        int idx = 1;
        int totalUnits = 0;
        decimal totalSumWithoutVat = 0m;

        bool zebra = false;
        foreach (var item in movement.Items)
        {
            decimal rowTotal = item.Quantity * item.UnitPrice;
            totalUnits += item.Quantity;
            totalSumWithoutVat += rowTotal;

            if (zebra)
            {
                gfx.DrawRectangle(zebraBg, margin, currentY, pageWidth, 18);
            }
            gfx.DrawRectangle(borderPen, margin, currentY, pageWidth, 18);

            string sku = item.Product?.Sku ?? "-";
            string uktZed = item.Product?.CategoryId == 1 ? "8537 10 91 00" : "8504 40 30 00";
            string name = item.Product?.Name ?? "Товар";
            if (name.Length > 38) name = name[..36] + "...";
            string unit = item.Product?.Unit ?? "шт";

            x = margin + 3;
            gfx.DrawString(idx.ToString(), smallFont, secondaryBrush, x, currentY + 12); x += colNo;
            gfx.DrawString(sku, smallFont, primaryBrush, x, currentY + 12); x += colSku;
            gfx.DrawString(uktZed, smallFont, mutedBrush, x, currentY + 12); x += colUkt;
            gfx.DrawString(name, regularFont, primaryBrush, x, currentY + 12); x += colName;
            gfx.DrawString(unit, smallFont, secondaryBrush, x, currentY + 12); x += colUnit;
            gfx.DrawString(item.Quantity.ToString(), boldFont, primaryBrush, x, currentY + 12); x += colQty;
            gfx.DrawString(item.UnitPrice.ToString("N2"), regularFont, primaryBrush, x, currentY + 12); x += colPrice;
            gfx.DrawString(rowTotal.ToString("N2"), boldFont, primaryBrush, x, currentY + 12);

            currentY += 18;
            idx++;
            zebra = !zebra;

            if (currentY > page.Height - 160)
            {
                page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                currentY = margin;
            }
        }

        // 5. Totals & Tax Calculation
        decimal vat = Math.Round(totalSumWithoutVat * 0.20m, 2);
        decimal totalWithVat = totalSumWithoutVat + vat;

        currentY += 4;
        gfx.DrawLine(borderPen, margin + pageWidth - 200, currentY, margin + pageWidth, currentY);
        currentY += 12;

        gfx.DrawString("Разом без ПДВ:", boldFont, secondaryBrush, margin + pageWidth - 190, currentY);
        gfx.DrawString($"{totalSumWithoutVat:N2} грн", boldFont, primaryBrush, margin + pageWidth - 70, currentY);
        currentY += 13;

        gfx.DrawString("ПДВ (20%):", regularFont, secondaryBrush, margin + pageWidth - 190, currentY);
        gfx.DrawString($"{vat:N2} грн", regularFont, primaryBrush, margin + pageWidth - 70, currentY);
        currentY += 15;

        gfx.DrawRectangle(tableHeaderBg, margin + pageWidth - 200, currentY - 11, 200, 18);
        gfx.DrawRectangle(borderPen, margin + pageWidth - 200, currentY - 11, 200, 18);
        gfx.DrawString("ВСЬОГО З ПДВ:", boldFont, primaryBrush, margin + pageWidth - 190, currentY + 2);
        gfx.DrawString($"{totalWithVat:N2} грн", subHeaderFont, primaryBrush, margin + pageWidth - 75, currentY + 2);
        currentY += 24;

        // 6. Textual Summary (Сума прописом)
        string itemsText = UkrainianNumberToWords.NumberOfItemsToWords(movement.Items.Count);
        string sumWords = UkrainianNumberToWords.AmountToWords(totalWithVat);

        gfx.DrawString($"Всього відпущено найменувань: {itemsText}, загальною кількістю {totalUnits} од.", boldFont, primaryBrush, margin, currentY);
        currentY += 13;
        gfx.DrawString($"На суму: {sumWords}", boldFont, primaryBrush, margin, currentY);
        currentY += 11;
        gfx.DrawString($"в т.ч. ПДВ 20%: {vat:N2} грн.", regularFont, secondaryBrush, margin, currentY);
        currentY += 18;

        // 7. Legal Disclaimer
        gfx.DrawLine(borderPen, margin, currentY, margin + pageWidth, currentY);
        currentY += 9;
        gfx.DrawString("Товарно-матеріальні цінності передані у належному стані, без видимих механічних пошкоджень, у повній комплектності.", italicFont, mutedBrush, margin, currentY);
        currentY += 9;
        gfx.DrawString("Претензії щодо кількості та якості приймаються протягом трьох робочих днів з моменту підписання цієї накладної.", italicFont, mutedBrush, margin, currentY);
        currentY += 20;

        // 8. Signatures, Power of Attorney & Seals
        double colSig = pageWidth / 2;

        gfx.DrawString("ВІДПУСТИВ (ЗДАВ):", boldFont, primaryBrush, margin, currentY);
        gfx.DrawString("ОТРИМАВ (ПРИЙНЯВ):", boldFont, primaryBrush, margin + colSig, currentY);
        currentY += 14;

        gfx.DrawString("Завідувач складу / Комірник:", regularFont, secondaryBrush, margin, currentY);
        gfx.DrawString("Представник одержувача:", regularFont, secondaryBrush, margin + colSig, currentY);
        currentY += 16;

        gfx.DrawString("______________________ / " + movement.StaffLogin + " /", boldFont, primaryBrush, margin, currentY);
        gfx.DrawString("______________________ / " + (movement.CustomerName ?? "Петренко С. М.") + " /", boldFont, primaryBrush, margin + colSig, currentY);
        currentY += 16;

        gfx.DrawString("М. П.                                     (підпис)", smallFont, mutedBrush, margin, currentY);
        gfx.DrawString("М. П.                                     (підпис)", smallFont, mutedBrush, margin + colSig, currentY);
        currentY += 14;

        gfx.DrawString("Дата фактичної видачі: " + movement.CreatedAt.ToString("dd.MM.yyyy") + " р.", smallFont, secondaryBrush, margin, currentY);
        gfx.DrawString("За довіреністю: серія ААБ № 412850 від " + movement.CreatedAt.AddDays(-2).ToString("dd.MM.yyyy") + " р.", smallFont, secondaryBrush, margin + colSig, currentY);

        using var memoryStream = new MemoryStream();
        document.Save(memoryStream, false);
        return memoryStream.ToArray();
    }
}
