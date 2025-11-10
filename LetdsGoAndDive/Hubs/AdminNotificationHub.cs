using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace LetdsGoAndDive.Hubs
{
    public class AdminNotificationHub : Hub
    {
        
        public async Task SendNewOrderAlert(string message)
        {
            await Clients.All.SendAsync("ReceiveNewOrderAlert", message);
        }
    }
}
