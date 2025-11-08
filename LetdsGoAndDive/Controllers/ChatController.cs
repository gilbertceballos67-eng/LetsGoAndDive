using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LetdsGoAndDive.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace LetdsGoAndDive.Controllers
{
    [Authorize]  // Users must be logged in
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var userEmail = currentUser.Email;

            // ✅ Fix 1: Match with "AdminGroup" (your Hub & Admin use this name)
            var unreadMessages = _context.Messages
                .Where(m => m.Receiver == userEmail && m.Sender == "AdminGroup" && !m.IsRead && !m.IsDeleted);

            foreach (var msg in unreadMessages)
                msg.IsRead = true;

            await _context.SaveChangesAsync();

            // ✅ Fix 2: Also match with AdminGroup for message history
            var messages = await _context.Messages
                .Where(m => !m.IsDeleted &&
                            ((m.Sender == userEmail && m.Receiver == "AdminGroup") ||
                             (m.Sender == "AdminGroup" && m.Receiver == userEmail)))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.User = userEmail; // ✅ Ensure email goes to the view (SignalR group key)
            return View(messages);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Json(0);

            var userEmail = currentUser.Email;

            int count = await _context.Messages
                .CountAsync(m => m.Receiver == userEmail && !m.IsRead && !m.IsDeleted);

            return Json(count);
        }
    }
}
