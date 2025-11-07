using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LetdsGoAndDive.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

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

        public async Task<IActionResult> Index()
        {
            // Only include users with non-deleted messages
            var users = await _context.Messages
                .Where(m => m.Sender != "Admin" && !m.IsDeleted)  // Filter out deleted messages
                .Select(m => new { Sender = m.Sender, UserId = m.Sender })
                .Distinct()
                .Join(_context.Users, m => m.Sender, u => u.Email, (m, u) => u.FullName ?? u.Email)
                .Distinct()
                .ToListAsync();

            return View(users);
        }

        public async Task<IActionResult> Chat(string user)
        {
            // Mark unread messages as read (only for non-deleted ones)
            var unreadMessages = _context.Messages
                .Where(m => m.Receiver == "Admin" && m.Sender == user && !m.IsRead && !m.IsDeleted);
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();

            // Load only non-deleted messages
            var messages = await _context.Messages
                .Where(m => !m.IsDeleted &&  // Filter out deleted messages
                            ((m.Sender == user && m.Receiver == "Admin") ||
                             (m.Sender == "Admin" && m.Receiver == user)))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.User = user;
            return View(messages);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            // Count only non-deleted unread messages
            var count = await _context.Messages
                .CountAsync(m => m.Receiver == "Admin" && !m.IsRead && !m.IsDeleted);

            return Json(count);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConversation(string user)
        {
            // First, try to find the email from the full name (case-insensitive)
            var userEmail = await _context.Users
                .Where(u => u.FullName.ToLower() == user.ToLower())
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            // If no match (e.g., FullName not set), assume 'user' is already the email
            if (userEmail == null)
            {
                userEmail = user;
            }

            // Soft delete: Mark ALL messages involving this user as deleted (instead of removing them)
            var messages = _context.Messages.Where(m =>
                (m.Sender == userEmail || m.Receiver == userEmail) && !m.IsDeleted);  // Only mark non-deleted ones

            if (messages.Any())
            {
                foreach (var msg in messages)
                {
                    msg.IsDeleted = true;
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
