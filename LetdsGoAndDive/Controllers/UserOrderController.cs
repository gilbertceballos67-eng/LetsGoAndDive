using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LetdsGoAndDive.Controllers
{
    [Authorize(Roles = nameof(Constants.Roles.Admin))]
    public class UserOrderController : Controller
    {
        private readonly IUserOrderRepository _userOrderRepo;

        public UserOrderController(IUserOrderRepository userOrderRepo)
        {
            _userOrderRepo = userOrderRepo;
        }
        public async Task<IActionResult> UserOrders()
        {
            var orders =await _userOrderRepo.UserOrders();
            return View(orders);
        }

        public async Task<IActionResult> CleanupOldOrders()
        {
            int deletedCount = await _userOrderRepo.PermanentlyDeleteOldOrders();
            TempData["msg"] = $"{deletedCount} old orders permanently deleted.";
            return RedirectToAction(nameof(UserOrders));
        }
    }
}
