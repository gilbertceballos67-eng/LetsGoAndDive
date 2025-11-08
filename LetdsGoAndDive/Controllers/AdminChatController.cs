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

        // ✅ Show list of users who messaged the admin
        public async Task<IActionResult> Index()
        {
            var users = await _context.Messages
                .Where(m => m.Sender != "AdminGroup" && !m.IsDeleted)
                .Select(m => new { Sender = m.Sender })
                .Distinct()
                .Join(_context.Users, m => m.Sender, u => u.Email, (m, u) => u.FullName ?? u.Email)
                .Distinct()
                .ToListAsync();

            return View(users);
        }

        // ✅ Chat view for a selected user
        public async Task<IActionResult> Chat(string user)
        {
            if (string.IsNullOrEmpty(user))
                return RedirectToAction("Index");

            // Always resolve email (since SignalR uses email as unique key)
            var userEmail = await _context.Users
                .Where(u => u.FullName == user || u.Email == user || u.UserName == user)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            userEmail ??= user; // fallback if not found

            // ✅ Mark unread as read
            var unreadMessages = await _context.Messages
                .Where(m => m.Receiver == "AdminGroup" && m.Sender == userEmail && !m.IsRead && !m.IsDeleted)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                    msg.IsRead = true;

                await _context.SaveChangesAsync();
            }

            // ✅ Get full chat history (not deleted)
            var messages = await _context.Messages
                .Where(m => !m.IsDeleted &&
                            ((m.Sender == userEmail && m.Receiver == "AdminGroup") ||
                             (m.Sender == "AdminGroup" && m.Receiver == userEmail)))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.User = userEmail;
            return View(messages);
        }

        // ✅ Return unread message count for admin dashboard
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _context.Messages
                .CountAsync(m => m.Receiver == "AdminGroup" && !m.IsRead && !m.IsDeleted);

            return Json(count);
        }

        // ✅ Delete conversation (mark all messages as deleted)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConversation(string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                return RedirectToAction("Index");

            // Get email properly
            var lowerUser = user.ToLowerInvariant();
            var userEmail = await _context.Users
                .Where(u =>
                    (u.FullName != null && u.FullName.ToLower() == lowerUser) ||
                    (u.Email != null && u.Email.ToLower() == lowerUser) ||
                    (u.UserName != null && u.UserName.ToLower() == lowerUser))
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            userEmail ??= user;

            // ✅ Soft delete all messages for that user
            var messages = await _context.Messages
                .Where(m => (m.Sender == userEmail || m.Receiver == userEmail) && !m.IsDeleted)
                .ToListAsync();

            if (messages.Any())
            {
                foreach (var msg in messages)
                    msg.IsDeleted = true;

                await _context.SaveChangesAsync();
            }

            // ✅ Optional: Notify connected clients
            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<ChatHub>>();
            await hubContext.Clients.Group(userEmail).SendAsync("ConversationDeleted", userEmail);

            return RedirectToAction("Index");
        }
    }
}
