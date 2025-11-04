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
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartController(ICartRepository cartRepo, ApplicationDbContext db, UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor)
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
                var cartCount = await _cartRepo.AddItem(productId, qty);
                return Json(new { success = true, count = cartCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> RemoveItem(int productId)
        {
            await _cartRepo.RemoveItem(productId);
            return RedirectToAction("GetUserCart");
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

        [AllowAnonymous]
        public async Task<IActionResult> GetTotalItemInCart()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Ok(0);

            int total = await _cartRepo.GetCartItemCount(userId);
            return Ok(total);
        }

        public IActionResult Checkout()
        {
            return View();
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