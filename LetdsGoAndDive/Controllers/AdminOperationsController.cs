using Microsoft.AspNetCore.Mvc;
using LetdsGoAndDive.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace LetdsGoAndDive.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class AdminOperationsController : Controller
    {
        private readonly IUserOrderRepository _userOrderRepository;
        private readonly IProductRepository _productRepo;
        private readonly ApplicationDbContext _context;


        public AdminOperationsController(IUserOrderRepository userOrderRepository, IProductRepository productRepo,
    ApplicationDbContext context)
        {
            _userOrderRepository = userOrderRepository;
            _productRepo = productRepo;
            _context = context;
        }

        public async Task<IActionResult> AllOrders(int page = 1, int pageSize = 8)
        {
            var orders = (await _userOrderRepository.UserOrders(true))
                   .Where(o => !o.IsArchived); 


            int totalItems = orders.Count();
            var pagedOrders = orders
                .OrderByDescending(o => o.CreateDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(pagedOrders);
        }


        [HttpPost]
        public async Task<IActionResult> TogglePaymentStatus(int orderId)
        {
            var order = await _userOrderRepository.GetOrderById(orderId);
            if (order == null)
                return NotFound();

            //  Toggle the IsPaid field
            order.IsPaid = !order.IsPaid;
            await _userOrderRepository.UpdateOrder(order);

            //  Send invoice only when order becomes Paid
            if (order.IsPaid)
            {
                Task.Run(() => SendInvoiceEmail(order));
            }


            TempData["msg"] = $"Payment status updated to {(order.IsPaid ? "Paid" : "Not Paid")}.";

            return RedirectToAction(nameof(AllOrders));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateOrderStatus(int orderId)
        {
            var order = await _userOrderRepository.GetOrderById(orderId);
            if (order == null)
            {
                TempData["msg"] = "Order not found.";
                return RedirectToAction("AllOrders");
            }

            ViewBag.OrderStatusList = (await _userOrderRepository.GetOrderStatuses())
                .Select(status => new SelectListItem
                {
                    Value = status.Id.ToString(),
                    Text = status.StatusName,
                    Selected = status.Id == order.OrderStatusId
                }).ToList();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(Order data)
        {
            var order = await _userOrderRepository.GetOrderById(data.Id);
            if (order == null)
            {
                TempData["msg"] = "Order not found.";
                return RedirectToAction(nameof(AllOrders));
            }

            // Update status
            order.OrderStatusId = data.OrderStatusId;

            bool feeUpdated = false;

            // Delivery Fee Update
            if (data.DeliveryFee.HasValue)
            {
                order.DeliveryFee = data.DeliveryFee.Value;
                feeUpdated = true;
            }

            // If Shipped, require DeliveryLink
            var status = (await _userOrderRepository.GetOrderStatuses())
                            .FirstOrDefault(s => s.Id == data.OrderStatusId);

            if (status != null && status.StatusName.Equals("Shipped", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(data.DeliveryLink))
                {
                    order.DeliveryLink = data.DeliveryLink;
                }
                else
                {
                    TempData["msg"] = "Please enter a delivery link before marking as shipped.";

                    ViewBag.OrderStatusList = (await _userOrderRepository.GetOrderStatuses())
                        .Select(s => new SelectListItem
                        {
                            Value = s.Id.ToString(),
                            Text = s.StatusName,
                            Selected = s.Id == data.OrderStatusId
                        }).ToList();

                    return View(order);
                }
            }

            await _userOrderRepository.UpdateOrder(order);

            // 🔔 SEND MESSAGE TO USER IF DELIVERY FEE WAS UPDATED
            if (feeUpdated)
            {
                var message = new Message
                {
                    Sender = "AdminGroup",
                    Receiver = order.Email,  // user email
                    Text = $"📦 Update on your order #{order.Id}: Delivery fee has been set to ₱{order.DeliveryFee}.",
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
            }

            TempData["msg"] = "Updated successfully";
            return RedirectToAction(nameof(UpdateOrderStatus), new { orderId = order.Id });
        }




        [HttpPost]
        public async Task<IActionResult> ArchiveOrder(int orderId)
        {
            var order = await _userOrderRepository.GetOrderById(orderId);
            if (order == null)
                return NotFound();

            order.IsArchived = true;
            await _userOrderRepository.UpdateOrder(order);

            TempData["msg"] = "Order archived successfully!";
            return RedirectToAction("AllOrders");
        }

        public async Task<IActionResult> ArchivedOrders(int page = 1, int pageSize = 10)
        {
            var archived = (await _userOrderRepository.UserOrders(true))
                            .Where(o => o.IsArchived)
                            .OrderByDescending(o => o.CreateDate)
                            .ToList();

            int totalItems = archived.Count;

            var pagedOrders = archived
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(pagedOrders);
        }





        // Send invoice email method
        private void SendInvoiceEmail(Order order)
        {
            try
            {
                string fromEmail = "letsgoanddive.invoice@gmail.com";
                string appPassword = "szvj irqv wppn ydrc"; // Gmail App Password
                string subject = "Payment Confirmation & Invoice - Let's Go And Dive";

                // Build item list table
                var orderItemsHtml = string.Join("", order.OrderDetail.Select(item => $@"
            <tr>
                <td style='border:1px solid #ddd;padding:8px;'>{item.Product?.ProductName ?? "N/A"}</td>
                <td style='border:1px solid #ddd;padding:8px;text-align:center;'>{item.Quantity}</td>
                <td style='border:1px solid #ddd;padding:8px;text-align:right;'>₱{item.UnitPrice:F2}</td>
                <td style='border:1px solid #ddd;padding:8px;text-align:right;'>₱{item.Quantity * item.UnitPrice:F2}</td>
            </tr>
        "));

                double totalAmount = order.OrderDetail.Sum(i => i.Quantity * i.UnitPrice);

                string body = $@"
                             <div style='font-family: Arial, sans-serif; color:#333;'>
                                  <h2>Hi {order.Name},</h2>
                                     <p>Thank you for your payment!</p>
                                     <p>Here are your order details:</p>
                                     <ul>
                                        <li><b>Order Date:</b> {order.CreateDate:MMMM dd, yyyy}</li>
                                        <li><b>Payment Method:</b> {order.PaymentMethod}</li>
                                       <li><b>Address:</b> {order.Address}</li>
                                     </ul>

                                    <h3>🛍️ Order Items:</h3>
                                    <table style='border-collapse:collapse;width:100%;margin-top:10px;'>
                                     <thead style='background-color:#f2f2f2;'>
                                        <tr>
                                           <th style='border:1px solid #ddd;padding:8px;text-align:left;'>Item</th>
                                           <th style='border:1px solid #ddd;padding:8px;text-align:center;'>Qty</th>
                                           <th style='border:1px solid #ddd;padding:8px;text-align:right;'>Unit Price</th>
                                          <th style='border:1px solid #ddd;padding:8px;text-align:right;'>Total</th>
                                       </tr>
                                          </thead>
                                          <tbody>
                                                  {orderItemsHtml}
                                          </tbody>
                                        <tfoot>
                                            <tr>
                                                 <td colspan='3' style='border:1px solid #ddd;padding:8px;text-align:right;'><b>Grand Total:</b></td>
                                                 <td style='border:1px solid #ddd;padding:8px;text-align:right;'><b>₱{totalAmount:F2}</b></td>
                                            </tr>
                                      </tfoot>
                                    </table>

                                    <p style='margin-top:20px;'><b>Status:</b> <span style='color:green;'>Paid ✅</span></p>
                                    <p>We appreciate your business!</p>
                                    <p>— Let's Go And Dive Team</p>
                               </div>";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, "Let's Go And Dive");
                mail.To.Add(order.Email);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(fromEmail, appPassword);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email: " + ex.Message);
            }


        }

        public async Task<IActionResult> Archived()
        {
            var archived = await _productRepo.GetArchivedProducts();
            return View(archived);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreOrder(int orderId)
        {
            var order = await _userOrderRepository.GetOrderById(orderId);
            if (order == null) return NotFound();

            order.IsArchived = false;
            await _userOrderRepository.UpdateOrder(order);

            TempData["msg"] = "Order restored successfully!";
            return RedirectToAction("ArchivedOrders");
        }



    }
}
