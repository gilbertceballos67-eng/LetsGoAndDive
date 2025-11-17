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
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

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


        //  SALES DASHBOARD

        public IActionResult Index(string month, string year)
        {
         
            ViewBag.Months = DateTimeFormatInfo
                .InvariantInfo
                .MonthNames
                .Where(m => !string.IsNullOrEmpty(m))
                .Select((m, i) => new SelectListItem
                {
                    Text = m,
                    Value = (i + 1).ToString()
                })
                .ToList();

          
            var years = _context.Orders
                .Select(o => o.CreateDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();
            ViewBag.Years = years;

           
            var orders = _context.Orders
                .Include(o => o.OrderDetail)
                .Include(o => o.OrderStatus)
                .AsQueryable();


            if (!string.IsNullOrEmpty(month))
            {
                int monthNum = int.Parse(month);
                orders = orders.Where(o => o.CreateDate.Month == monthNum);
                ViewBag.Month = monthNum;
            }

            if (!string.IsNullOrEmpty(year))
            {
                int yearNum = int.Parse(year);
                orders = orders.Where(o => o.CreateDate.Year == yearNum);
                ViewBag.Year = yearNum;
            }

         
            var orderList = orders.OrderByDescending(o => o.CreateDate).ToList();


            decimal totalSales = orderList.Sum(o => o.OrderDetail.Sum(d => (decimal)(d.Quantity * d.UnitPrice)));

            ViewBag.TotalSales = totalSales;

            
            ViewBag.YearlySales = _context.Orders
                .Include(o => o.OrderDetail)
                .GroupBy(o => o.CreateDate.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    Total = g.Sum(o => o.OrderDetail.Sum(d => (decimal)(d.Quantity * d.UnitPrice)))

                })
                .OrderBy(x => x.Year)
                .ToList();

            return View(orderList);
        }


        //  EXPORT TO PDF

        public async Task<IActionResult> ExportToPdf(int? month, int? year, string exportedBy = "Unknown")
        {
            var orders = _context.Orders
                .Include(o => o.OrderDetail)
                .Include(o => o.OrderStatus)
                .Where(o => !o.IsDeleted && o.IsPaid)
                .AsQueryable();

            if (year.HasValue)
            {
                orders = orders.Where(o => o.CreateDate.Year == year);

                if (month.HasValue && month.Value > 0)
                    orders = orders.Where(o => o.CreateDate.Month == month);
            }

            var orderList = await orders.ToListAsync();
            double total = orderList.Sum(o => o.OrderDetail?.Sum(d => d.Quantity * d.UnitPrice) ?? 0);

            if (!orderList.Any())
            {
                TempData["error"] = "No sales found for this selected period.";
                return RedirectToAction(nameof(Index), new { month, year });
            }

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 40, 40, 50, 50);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                var titleFont = FontFactory.GetFont("Helvetica", 18, Font.BOLD);
                var subFont = FontFactory.GetFont("Helvetica", 12, Font.NORMAL);
                var boldFont = FontFactory.GetFont("Helvetica", 12, Font.BOLD);

                string monthLabel = month.HasValue && month.Value > 0
                    ? System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month.Value)
                    : "All Months";
                string yearLabel = year?.ToString() ?? "All Years";

                Paragraph title = new Paragraph($"Let's Go and Dive\nSales Report", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                };
                pdfDoc.Add(title);

                // Updated section with exporter name
                Paragraph dateInfo = new Paragraph(
                    $"Month: {monthLabel} / Year: {yearLabel}\n" +
                    $"Generated on: {DateTime.Now:MMMM dd, yyyy hh:mm tt}\n" +
                    $"Exported By: {exportedBy}\n\n",
                    subFont
                );
                pdfDoc.Add(dateInfo);

                PdfPTable lineTable = new PdfPTable(1) { WidthPercentage = 100 };
                PdfPCell lineCell = new PdfPCell(new Phrase(""))
                {
                    BorderWidthBottom = 1,
                    BorderColorBottom = new BaseColor(150, 150, 150),
                    FixedHeight = 5,
                    Border = Rectangle.BOTTOM_BORDER
                };
                lineTable.AddCell(lineCell);
                pdfDoc.Add(lineTable);
                pdfDoc.Add(new Paragraph(" "));

                PdfPTable table = new PdfPTable(5)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 10f,
                    SpacingAfter = 10f
                };
                table.SetWidths(new float[] { 1.2f, 2.2f, 1.5f, 2f, 1.3f });

                BaseColor headerColor = new BaseColor(33, 150, 243);
                BaseColor altRowColor = new BaseColor(245, 245, 245);
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
                    table.AddCell(new PdfPCell(new Phrase($"₱{orderTotal:N2}", subFont)) { BackgroundColor = bg, Padding = 6, HorizontalAlignment = Element.ALIGN_RIGHT });
                }

                pdfDoc.Add(table);

                Paragraph totalParagraph = new Paragraph($"Total Sales: ₱{total:N2}", boldFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingBefore = 10f
                };
                pdfDoc.Add(totalParagraph);

                pdfDoc.Add(new Paragraph("\nThank you for managing your sales with Let's Go and Dive!", subFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 20f
                });

                pdfDoc.Close();
                writer.Close();

                string fileName = $"SalesReport_{monthLabel}_{yearLabel}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
        }



        //  RESET SALES

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
