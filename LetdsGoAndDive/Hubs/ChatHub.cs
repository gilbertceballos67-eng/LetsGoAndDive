using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using LetdsGoAndDive.Data;
using LetdsGoAndDive.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LetdsGoAndDive.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            if (user.IsInRole("Admin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admin");
            }
            else
            {
                var currentUser = await _userManager.GetUserAsync(user);
                var userFullName = currentUser?.FullName ?? user.Identity.Name;
                await Groups.AddToGroupAsync(Context.ConnectionId, userFullName);
            }
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(string sender, string message, string receiver)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var msg = new Message
            {
                Sender = sender,
                Receiver = receiver,
                Text = message,
                IsRead = false  
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            
            if (receiver == "Admin")
            {
                await Clients.Group("Admin").SendAsync("ReceiveMessage", sender, message);
                
                var adminUnreadCount = await _context.Messages.CountAsync(m => m.Receiver == "Admin" && !m.IsRead);
                await Clients.Group("Admin").SendAsync("UpdateUnreadCount", adminUnreadCount);
            }
            else
            {
                await Clients.Group(receiver).SendAsync("ReceiveMessage", sender, message);
                
                var userUnreadCount = await _context.Messages.CountAsync(m => m.Receiver == receiver && m.Sender == "Admin" && !m.IsRead);
                await Clients.Group(receiver).SendAsync("UpdateUnreadCount", userUnreadCount);
            }
        }
    }
}