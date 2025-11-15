using iTextSharp.text;
using iTextSharp.text.pdf;

using LetdsGoAndDive.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class InventoryReportController : Controller
{
    private readonly IInventoryRepository _inventoryRepo;

    public InventoryReportController(IInventoryRepository inventoryRepo)
    {
        _inventoryRepo = inventoryRepo;
    }

    public async Task<IActionResult> Index(string search = "", DateTime? from = null, DateTime? to = null, int? itemTypeId = null)
    {
        ViewBag.Search = search;
        ViewBag.From = from?.ToString("yyyy-MM-dd") ?? "";
        ViewBag.To = to?.ToString("yyyy-MM-dd") ?? "";
        ViewBag.ItemTypeId = itemTypeId;

        var stockSummary = await _inventoryRepo.GetStockSummary(search);
        var sales = await _inventoryRepo.GetSalesByProduct(from, to, itemTypeId);

        var model = new InventoryDashboardViewModel
        {
            StockSummary = stockSummary.ToList(),
            SalesByProduct = sales.ToList()
        };

        return View(model);
    }

    // Chart Data
    [HttpGet]
    public async Task<IActionResult> ChartData(DateTime? from = null, DateTime? to = null, int? itemTypeId = null)
    {
        var sales = await _inventoryRepo.GetSalesByProduct(from, to, itemTypeId);
        var stock = await _inventoryRepo.GetStockSummary("");

        var result = new
        {
            sales = sales.Select(s => new { s.ProductName, s.QuantitySold, s.Revenue }),
            stock = stock.Select(s => new { s.ProductName, s.Quantity })
        };

        return Ok(result);
    }

    // Excel Export
    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateTime? from = null, DateTime? to = null, int? itemTypeId = null)
    {
        var sales = await _inventoryRepo.GetSalesByProduct(from, to, itemTypeId);
        var stock = await _inventoryRepo.GetStockSummary("");

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Inventory Report");

        ws.Cells["A1"].Value = "Product";
        ws.Cells["B1"].Value = "Current Stock";
        ws.Cells["C1"].Value = "Quantity Sold";
        ws.Cells["D1"].Value = "Revenue";

        int row = 2;

        foreach (var s in stock)
        {
            var sold = sales.FirstOrDefault(x => x.ProductId == s.ProductId);

            ws.Cells[row, 1].Value = s.ProductName;
            ws.Cells[row, 2].Value = s.Quantity;
            ws.Cells[row, 3].Value = sold?.QuantitySold ?? 0;
            ws.Cells[row, 4].Value = sold?.Revenue ?? 0;

            row++;
        }

        ws.Cells[1, 1, 1, 4].Style.Font.Bold = true;
        ws.Cells[1, 1, row - 1, 4].AutoFitColumns();

        var data = pkg.GetAsByteArray();

        return File(data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"InventoryReport_{DateTime.UtcNow:yyyyMMddHHmm}.xlsx");
    }

    // PDF export (iTextSharp)
    [HttpGet]
    public async Task<IActionResult> ExportPdf(DateTime? from = null, DateTime? to = null, int? itemTypeId = null)
    {
        var sales = await _inventoryRepo.GetSalesByProduct(from, to, itemTypeId);
        var stock = await _inventoryRepo.GetStockSummary("");

        using (MemoryStream stream = new MemoryStream())
        {
            // A4 PDF
            Document pdfDoc = new Document(PageSize.A4, 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
            pdfDoc.Open();

            // Title
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            pdfDoc.Add(new Paragraph("Inventory Report", titleFont));
            pdfDoc.Add(new Paragraph("\n"));

            // Table with 4 columns
            PdfPTable table = new PdfPTable(4);
            table.WidthPercentage = 100;
            float[] widths = new float[] { 35f, 20f, 20f, 25f };
            table.SetWidths(widths);

            // Header style
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);

            table.AddCell(new PdfPCell(new Phrase("Product", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("Current Stock", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("Quantity Sold", headerFont)));
            table.AddCell(new PdfPCell(new Phrase("Revenue", headerFont)));

            // Add rows
            foreach (var s in stock)
            {
                var sold = sales.FirstOrDefault(x => x.ProductName == s.ProductName);

                table.AddCell(s.ProductName);
                table.AddCell(s.Quantity.ToString());
                table.AddCell((sold?.QuantitySold ?? 0).ToString());
                table.AddCell((sold?.Revenue ?? 0).ToString("N2"));
            }

            pdfDoc.Add(table);
            pdfDoc.Close();

            return File(stream.ToArray(), "application/pdf",
                $"InventoryReport_{DateTime.UtcNow:yyyyMMddHHmm}.pdf");
        }
    }
    
}
