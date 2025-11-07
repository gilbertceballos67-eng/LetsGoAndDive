using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LetdsGoAndDive.Data;
using LetdsGoAndDive.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace LetdsGoAndDive.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SalesReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SalesReportController> _logger;

        public SalesReportController(ApplicationDbContext context, ILogger<SalesReportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Dashboard view
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var orders = _context.Orders
                .Include(o => o.OrderDetail)
                .Include(o => o.OrderStatus)
                .Where(o => !o.IsDeleted && o.IsPaid)
                .AsQueryable();

            if (month.HasValue && year.HasValue)
            {
                orders = orders.Where(o => o.CreateDate.Month == month && o.CreateDate.Year == year);
            }

            var orderList = await orders.OrderByDescending(o => o.CreateDate).ToListAsync();

            
            var totalSales = orderList.Sum(o => o.OrderDetail?.Sum(d => d.Quantity * d.UnitPrice) ?? 0);

            ViewBag.Month = month;
            ViewBag.Year = year;
            ViewBag.TotalSales = totalSales;
            ViewBag.Months = Enumerable.Range(1, 12)
                .Select(i => new { Value = i, Text = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i) })
                .ToList();
            ViewBag.Years = Enumerable.Range(DateTime.Now.Year - 5, 6).Reverse().ToList();

            return View(orderList);
        }


        public async Task<IActionResult> ExportToPdf(int? month, int? year)
        {
            var orders = _context.Orders
                .Include(o => o.OrderDetail)
                .Include(o => o.OrderStatus)
                .Where(o => !o.IsDeleted && o.IsPaid)
                .AsQueryable();

            if (month.HasValue && year.HasValue)
            {
                orders = orders.Where(o => o.CreateDate.Month == month && o.CreateDate.Year == year);
            }

            var orderList = await orders.ToListAsync();
            double total = orderList.Sum(o => o.OrderDetail?.Sum(d => d.Quantity * d.UnitPrice) ?? 0);

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 40, 40, 50, 50);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                // ================== HEADER ==================
                var titleFont = FontFactory.GetFont("Helvetica", 18, Font.BOLD);
                var subFont = FontFactory.GetFont("Helvetica", 12, Font.NORMAL);
                var boldFont = FontFactory.GetFont("Helvetica", 12, Font.BOLD);

                Paragraph title = new Paragraph("Let's Go and Dive\nMonthly Sales Report", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                };
                pdfDoc.Add(title);

                Paragraph dateInfo = new Paragraph(
                    $"Month: {(month.HasValue ? System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month.Value) : "All")} / " +
                    $"Year: {(year?.ToString() ?? "All")}\n" +
                    $"Generated on: {DateTime.Now:MMMM dd, yyyy}\n\n",
                    subFont
                );
                pdfDoc.Add(dateInfo);

                // ============= LINE SEPARATOR (Manual for iTextSharp 5) =============
                PdfPTable lineTable = new PdfPTable(1);
                lineTable.WidthPercentage = 100;
                PdfPCell lineCell = new PdfPCell(new Phrase(""))
                {
                    BorderWidthBottom = 1,
                    BorderWidthTop = 0,
                    BorderWidthLeft = 0,
                    BorderWidthRight = 0,
                    BorderColorBottom = new BaseColor(150, 150, 150),
                    FixedHeight = 5
                };
                lineTable.AddCell(lineCell);
                pdfDoc.Add(lineTable);
                pdfDoc.Add(new Paragraph(" "));

                // ================== TABLE ==================
                PdfPTable table = new PdfPTable(5)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 10f,
                    SpacingAfter = 10f
                };
                table.SetWidths(new float[] { 1.2f, 2.2f, 1.5f, 2f, 1.3f });

                BaseColor headerColor = new BaseColor(33, 150, 243); // Blue
                BaseColor altRowColor = new BaseColor(245, 245, 245); // Light gray
                BaseColor white = new BaseColor(255, 255, 255);

                string[] headers = { "Date", "Customer", "Status", "Payment Method", "Total (₱)" };
                foreach (string header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, FontFactory.GetFont("Helvetica", 11, Font.BOLD, white)))

                    {
                        BackgroundColor = headerColor,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 8
                    };
                    table.AddCell(cell);
                }

                int rowIndex = 0;
                foreach (var order in orderList)
                {
                    double orderTotal = order.OrderDetail?.Sum(d => d.Quantity * d.UnitPrice) ?? 0;
                    BaseColor bg = (rowIndex++ % 2 == 0) ? white : altRowColor;

                    table.AddCell(new PdfPCell(new Phrase(order.CreateDate.ToLocalTime().ToString("MM/dd/yyyy"), subFont)) { BackgroundColor = bg, Padding = 6 });
                    table.AddCell(new PdfPCell(new Phrase(order.Name ?? "N/A", subFont)) { BackgroundColor = bg, Padding = 6 });
                    table.AddCell(new PdfPCell(new Phrase(order.OrderStatus?.StatusName ?? "Completed", subFont)) { BackgroundColor = bg, Padding = 6 });
                    table.AddCell(new PdfPCell(new Phrase(order.PaymentMethod ?? "N/A", subFont)) { BackgroundColor = bg, Padding = 6 });
                    table.AddCell(new PdfPCell(new Phrase($"₱{orderTotal:F2}", subFont)) { BackgroundColor = bg, Padding = 6, HorizontalAlignment = Element.ALIGN_RIGHT });
                }

                pdfDoc.Add(table);

                // ================== TOTAL ==================
                Paragraph totalParagraph = new Paragraph($"Total Sales: ₱{total:F2}", boldFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingBefore = 10f
                };
                pdfDoc.Add(totalParagraph);

                // ================== FOOTER ==================
                pdfDoc.Add(new Paragraph("\nThank you for managing your sales with Let's Go and Dive!", subFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 20f
                });

                pdfDoc.Close();
                writer.Close();

                return File(stream.ToArray(), "application/pdf", $"SalesReport_{month}_{year}.pdf");
            }
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetSales(int? month, int? year)
        {
            if (!month.HasValue || !year.HasValue)
            {
                TempData["error"] = "Please select a month and year to reset sales.";
                return RedirectToAction(nameof(Index));
            }

            var ordersToDelete = _context.Orders
                .Include(o => o.OrderDetail)
                .Where(o => o.CreateDate.Month == month && o.CreateDate.Year == year && o.IsPaid && !o.IsDeleted)
                .ToList();

            if (!ordersToDelete.Any())
            {
                TempData["info"] = $"No paid sales found for {month}/{year}.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var order in ordersToDelete)
            {
                order.IsDeleted = true;
            }

            await _context.SaveChangesAsync();

            TempData["success"] = $"Sales data for {month}/{year} has been reset successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
