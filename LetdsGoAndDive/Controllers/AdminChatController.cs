using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LetdsGoAndDive.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace LetdsGoAndDive.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Messages
                .Select(m => m.Sender)
                .Distinct()
                .Where(u => u != "Admin")
                .ToListAsync();

            return View(users);
        }

        public async Task<IActionResult> Chat(string user)
        {
            var messages = await _context.Messages
                .Where(m => (m.Sender == user && m.Receiver == "Admin") ||
                            (m.Sender == "Admin" && m.Receiver == user))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.User = user;
            return View(messages);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConversation(string user)
        {
            if (string.IsNullOrEmpty(user))
                return BadRequest();

            var messages = _context.Messages
                .Where(m => (m.Sender == user && m.Receiver == "Admin") ||
                            (m.Sender == "Admin" && m.Receiver == user));

            _context.Messages.RemoveRange(messages);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Conversation with {user} deleted successfully.";
            return RedirectToAction("Index");
        }

    }
}
