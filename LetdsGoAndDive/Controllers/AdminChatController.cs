using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LetdsGoAndDive.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using LetdsGoAndDive.Hubs;

namespace LetdsGoAndDive.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ FIXED: Now returns emails only (joins with Users to ensure validity)
        public async Task<IActionResult> Index()
        {
            var userEmails = await _context.Messages
                .Where(m => m.Sender != "AdminGroup" && !m.IsDeleted)
                .Join(_context.Users, m => m.Sender, u => u.Email, (m, u) => u.Email)  // ✅ Ensures Sender matches a real Email
                .Distinct()
                .ToListAsync();

            return View(userEmails);
        }

        // ✅ Chat view (unchanged, but now user param is always a valid email)
        public async Task<IActionResult> Chat(string user)
        {
            if (string.IsNullOrEmpty(user))
            {
                TempData["Error"] = "User email is required.";
                return RedirectToAction("Index");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Email == user);
            if (!userExists)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var unreadMessages = await _context.Messages
                .Where(m => m.Receiver == "AdminGroup" && m.Sender == user && !m.IsRead && !m.IsDeleted)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                    msg.IsRead = true;

                await _context.SaveChangesAsync();
            }

            var messages = await _context.Messages
                .Where(m => !m.IsDeleted &&
                            ((m.Sender == user && m.Receiver == "AdminGroup") ||
                             (m.Sender == "AdminGroup" && m.Receiver == user)))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.User = user;
            ViewBag.IsAdmin = true;
            return View(messages);
        }

        // ✅ Unchanged
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _context.Messages
                .CountAsync(m => m.Receiver == "AdminGroup" && !m.IsRead && !m.IsDeleted);

            return Json(count);
        }

        // ✅ Unchanged (already handles name/email mapping)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConversation(string user)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                TempData["Error"] = "User is required.";
                return RedirectToAction("Index");
            }

            var lowerUser = user.ToLowerInvariant();
            var userEmail = await _context.Users
                .Where(u =>
                    (u.FullName != null && u.FullName.ToLower() == lowerUser) ||
                    (u.Email != null && u.Email.ToLower() == lowerUser) ||
                    (u.UserName != null && u.UserName.ToLower() == lowerUser))
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var messages = await _context.Messages
                .Where(m => (m.Sender == userEmail || m.Receiver == userEmail) && !m.IsDeleted)
                .ToListAsync();

            if (messages.Any())
            {
                foreach (var msg in messages)
                    msg.IsDeleted = true;

                await _context.SaveChangesAsync();
            }

            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<ChatHub>>();
            await hubContext.Clients.Group(userEmail).SendAsync("conversationdeleted", userEmail);  // ✅ FIXED: Lowercase

            TempData["Success"] = "Conversation deleted.";
            return RedirectToAction("Index");
        }
    }
}