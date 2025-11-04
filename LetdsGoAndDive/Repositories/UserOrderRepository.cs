using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LetdsGoAndDive.Data;
using LetdsGoAndDive.Models;

namespace LetdsGoAndDive.Repositories
{
    public class UserOrderRepository : IUserOrderRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpcontextAccessor;

        public UserOrderRepository(ApplicationDbContext db,
                                   IHttpContextAccessor httpcontextAccessor,
                                   UserManager<IdentityUser> userManager)
        {
            _db = db;
            _httpcontextAccessor = httpcontextAccessor;
            _userManager = userManager;
        }

        public async Task ChangeOrderStatus(UpdateOrderStatusModel data)
        {
            var order = await _db.Orders.FindAsync(data.OrderId);
            if (order == null)
                throw new InvalidOperationException($"Order with id {data.OrderId} not found.");

            order.OrderStatusId = data.OrderStatusId;
            await _db.SaveChangesAsync();
        }

        public async Task<Order?> GetOrderById(int id)
        {
            return await _db.Orders
                 .Include(o => o.OrderDetail)
                 .ThenInclude(od => od.Product)
                 .ThenInclude(p => p.ItemType)
                 .Include(o => o.OrderStatus)
                 .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<OrderStatus>> GetOrderStatuses()
        {
            return await _db.orderStatuses.ToListAsync();
        }

        public async Task TogglePaymentStatus(int orderId)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null)
                throw new InvalidOperationException($"Order with id {orderId} not found.");

            order.IsPaid = !order.IsPaid;
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Order>> UserOrders(bool getAll = false)
        {
            var orders = _db.Orders
                           .Include(x => x.OrderStatus)
                           .Include(x => x.OrderDetail)
                           .ThenInclude(x => x.Product)
                           .ThenInclude(x => x.ItemType)
                           .AsQueryable();

            if (!getAll)
            {
                var userId = GetUserrId();
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("User is not logged in.");

                orders = orders.Where(a => a.UserId == userId);
            }

            return await orders.ToListAsync();
        }

        private string GetUserrId()
        {
            var principal = _httpcontextAccessor.HttpContext?.User;
            return _userManager.GetUserId(principal);
        }

        public async Task<bool> DeleteOrder(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderDetail)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return false;

            _db.OrderDetails.RemoveRange(order.OrderDetail);
            _db.Orders.Remove(order);

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task UpdateOrder(Order order)
        {
            var existingOrder = await _db.Orders.FirstOrDefaultAsync(o => o.Id == order.Id);
            if (existingOrder != null)
            {
                existingOrder.IsPaid = order.IsPaid;
                _db.Orders.Update(existingOrder);
                await _db.SaveChangesAsync();
            }
        }
    }
}
