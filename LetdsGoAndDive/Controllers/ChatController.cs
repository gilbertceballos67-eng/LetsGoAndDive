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
    public class ChatController : Controller  // Changed from UserChatController to ChatController
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
            var userEmail = currentUser.Email;

            // Mark unread messages as read (only for non-deleted ones)
            var unreadMessages = _context.Messages
                .Where(m => m.Receiver == userEmail && m.Sender == "Admin" && !m.IsRead && !m.IsDeleted);
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();

            // Load only non-deleted messages for this user
            var messages = await _context.Messages
                .Where(m => !m.IsDeleted &&
                            ((m.Sender == userEmail && m.Receiver == "Admin") ||
                             (m.Sender == "Admin" && m.Receiver == userEmail)))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.User = userEmail;  // Pass user email for the view
            return View(messages);  // Looks for Views/Chat/Index.cshtml
        }
    }
}