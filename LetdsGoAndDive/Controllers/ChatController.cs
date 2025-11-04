using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LetdsGoAndDive.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace LetdsGoAndDive.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = User.Identity.Name;

            var messages = await _context.Messages
                .Where(m => (m.Sender == user && m.Receiver == "Admin") ||
                            (m.Sender == "Admin" && m.Receiver == user))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.User = user;
            return View(messages);
        }
    }
}
