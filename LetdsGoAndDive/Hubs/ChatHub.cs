using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using LetdsGoAndDive.Data;
using LetdsGoAndDive.Models;

namespace LetdsGoAndDive.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string user, string message, string receiver = "Admin")
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var msg = new Message
            {
                Sender = user,
                Receiver = receiver,
                Text = message
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
