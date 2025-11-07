using LetdsGoAndDive.Models;
using LetdsGoAndDive.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LetdsGoAndDive.Data;

namespace LetdsGoAndDive.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartRepository _cartRepo;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartController(ICartRepository cartRepo, ApplicationDbContext db, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _cartRepo = cartRepo;
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }


        [Authorize]
        private string GetUserrId()
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            return _userManager.GetUserId(principal);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> AddItem(int productId, int qty = 1)
        {
            try
            {
                await _cartRepo.AddItem(productId, qty);
                int totalItems = await _cartRepo.GetCartItemCount(GetUserrId());

                
                return Json(new { success = true, totalItems });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            try
            {
                await _cartRepo.RemoveItem(productId);
                int totalItems = await _cartRepo.GetCartItemCount(GetUserrId());
                return Json(new { success = true, totalItems });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        public async Task<IActionResult> GetUserCart()
        {
            var userId = _userManager.GetUserId(User);
            var cart = await _cartRepo.GetUserCart(userId);

            // Debug: Check if cart items have product and itemtype loaded
            if (cart?.CartDetails != null)
            {
                foreach (var item in cart.CartDetails)
                {
                    if (item.Product == null)
                    {
                        Console.WriteLine($"DEBUG: Cart item {item.Id} has null Product");
                    }
                    else if (item.Product.ItemType == null)
                    {
                        Console.WriteLine($"DEBUG: Product {item.Product.Id} has null ItemType");
                    }
                }
            }

            return View(cart);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProceedSelectedCheckout([FromForm] int[] selectedItems)
        {
            if (selectedItems == null || selectedItems.Length == 0)
            {
                TempData["Error"] = "Please select at least one item to checkout.";
                return RedirectToAction(nameof(GetUserCart));
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var cart = await _cartRepo.GetUserCart(userId);
            if (cart == null || cart.CartDetails == null)
                return RedirectToAction(nameof(GetUserCart));

            cart.CartDetails = cart.CartDetails
                .Where(cd => selectedItems.Contains(cd.ProductId))
                .ToList();

            HttpContext.Session.SetString("SelectedCheckoutItems", string.Join(",", selectedItems));

        
            return RedirectToAction(nameof(Checkout));
        }


        [AllowAnonymous]
        public async Task<IActionResult> GetTotalItemInCart()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Ok(0);

            int total = await _cartRepo.GetCartItemCount(userId);
            return Ok(total);
        }

        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);

            var model = new CheckoutModel();

            if (user != null)
            {
                model.Name = user.FullName;
                model.Email = user.Email;
                model.MobileNumber = user.MobileNumber;
                model.Address = user.Address;
            }

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> CheckOut(CheckoutModel model, IFormFile? PaymentProofFile)
        {
            if (!ModelState.IsValid)
                return View(model);

            var order = await _cartRepo.DoCheckout(model);
            if (order == null)
                return RedirectToAction(nameof(OrderFailure));

           
            if (PaymentProofFile != null && PaymentProofFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{PaymentProofFile.FileName}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "payments", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await PaymentProofFile.CopyToAsync(stream);
                }

                order.ProofOfPaymentImagePath = $"/uploads/payments/{fileName}";
                _db.Orders.Update(order);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(OrderSucces), new { orderId = order.Id });
        }


        public async Task<IActionResult> OrderSucces(int orderId)
        {
            var order = await _cartRepo.GetOrderById(orderId);
            if (order == null)
                return RedirectToAction(nameof(OrderFailure));

            return View(order);
        }

        public IActionResult OrderFailure()
        {
            return View();
        }
    }
}