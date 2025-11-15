using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using LetdsGoAndDive.Data;
using LetdsGoAndDive.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

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
            try
            {
                var user = Context.User;
                string groupName = Context.ConnectionId;

                if (user?.Identity?.IsAuthenticated == true)
                {
                    var currentUser = await _userManager.GetUserAsync(user);
                    if (currentUser != null)
                    {
                        var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                        if (isAdmin)
                        {
                            groupName = "AdminGroup";
                        }
                        else
                        {
                            groupName = currentUser.Email ?? currentUser.UserName ?? currentUser.Id;
                        }
                    }
                }
                else
                {
                    // anonymous fallback (for safety)
                    groupName = Context.ConnectionId;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                Console.WriteLine($"[ChatHub] Connected user={groupName} ConnID={Context.ConnectionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHub.OnConnectedAsync] ERROR: {ex}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"[ChatHub] Disconnected ConnID={Context.ConnectionId} Reason={exception?.Message}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string sender, string message, string receiver)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(receiver))
                return;

            try
            {
                // ✅ Save message in DB
                var msg = new Message
                {
                    Sender = sender,
                    Receiver = receiver,
                    Text = message,
                    SentAt = DateTime.UtcNow,
                    IsRead = false,
                    IsDeleted = false
                };
                await _context.Messages.AddAsync(msg);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHub.SendMessage] DB error: {ex}");
                return;  // Don't send if save fails
            }

            try
            {
                Console.WriteLine($"[ChatHub] Sending from {sender} to group: {receiver}");
                // ✅ Send to receiver (main target) and sender (for echo)
                await Clients.Group(receiver).SendAsync("receivemessage", sender, message);
                await Clients.Group(sender).SendAsync("receivemessage", sender, message);

                // ✅ Update unread count for receiver
                var unreadCount = await _context.Messages.CountAsync(m => m.Receiver == receiver && !m.IsRead && !m.IsDeleted);
                await Clients.Group(receiver).SendAsync("UpdateUnreadCount", unreadCount);

                Console.WriteLine($"[ChatHub] Message sent successfully from {sender} to {receiver}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHub.SendMessage] SignalR send error: {ex}");
            }
        }


        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            Console.WriteLine($"[ChatHub] Manually joined group: {groupName} for ConnID={Context.ConnectionId}");
        }

        public async Task DeleteConversation(string targetReceiver)
        {
            try
            {
                var caller = Context.User;
                var currentUser = caller != null ? await _userManager.GetUserAsync(caller) : null;
                var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");
                if (!isAdmin)
                    return;

                // ✅ Soft delete messages
                var toDelete = await _context.Messages
                    .Where(m => m.Receiver == targetReceiver || m.Sender == targetReceiver)
                    .ToListAsync();

                if (toDelete.Any())
                {
                    foreach (var msg in toDelete)
                        msg.IsDeleted = true;

                    await _context.SaveChangesAsync();
                }

                // ✅ Notify both sides
                await Clients.Group("AdminGroup").SendAsync("conversationdeleted", targetReceiver);  
                await Clients.Group(targetReceiver).SendAsync("conversationdeleted", targetReceiver);

                Console.WriteLine($"[ChatHub] Deleted conversation with {targetReceiver}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHub.DeleteConversation] Error: {ex}");
            }
        }


        public async Task UpdateUnreadCount(string userEmail)
        {
            var count = await _context.Messages.CountAsync(m => m.Receiver == userEmail && !m.IsRead && !m.IsDeleted);
            await Clients.Group(userEmail).SendAsync("UpdateUnreadCount", count);
        }


        public async Task SendAdminNotice(string userEmail, string message)
        {
            if (string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(message))
                return;

            // Save in DB
            var msg = new Message
            {
                Sender = "AdminGroup",
                Receiver = userEmail,
                Text = message,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                IsDeleted = false
            };

            await _context.Messages.AddAsync(msg);
            await _context.SaveChangesAsync();

            // Send to user in real-time
            await Clients.Group(userEmail).SendAsync("receivemessage", "Admin", message);
        }

    }
}
