namespace LetdsGoAndDive.Repositories
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using LetdsGoAndDive.Data;
    using LetdsGoAndDive.Models;


    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpcontextAccessor;


        public CartRepository(ApplicationDbContext db, IHttpContextAccessor httpcontextAccessor,
            UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
            _httpcontextAccessor = httpcontextAccessor;
        }
     
        public async Task<int> AddItem(int productId, int qty)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                string userId = GetUserrId();
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("User is not logged in");

               
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
                if (product == null)
                    throw new Exception("Product not found");

              
                var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.ProductId == productId);
                var currentStock = stock == null ? 0 : stock.Quantity;

              
                Console.WriteLine($"DEBUG AddItem: productId={productId} productName={product.ProductName} stock={currentStock} qtyRequested={qty}");

                if (currentStock <= 0)
                    throw new Exception("Out of stock");

                if (qty > currentStock)
                    throw new Exception("Not enough stock available");

                var cart = await GetUserCart(userId);
                if (cart == null)
                {
                    cart = new Shoppingcart { UserId = userId };
                    _db.Shoppingcarts.Add(cart);
                    await _db.SaveChangesAsync();
                }

              
                var cartItem = await _db.CartDetails
                    .FirstOrDefaultAsync(a => a.ShoppingcartId == cart.Id && a.ProductId == productId);

                if (cartItem != null)
                {
                    if (cartItem.Quanntity + qty > currentStock)
                        throw new Exception("Not enough stock available");

                    cartItem.Quanntity += qty;
                }
                else
                {
                    cartItem = new CartDetail
                    {
                        ProductId = productId,
                        ShoppingcartId = cart.Id,
                        Quanntity = qty,
                        UnitPrice = product.Price
                    };
                    _db.CartDetails.Add(cartItem);
                }

              
                if (stock != null)
                {
                    stock.Quantity -= qty;
                    if (stock.Quantity < 0) stock.Quantity = 0;
                    _db.Stocks.Update(stock);
                }
                else
                {
                    
                    _db.Stocks.Add(new Stock { ProductId = productId, Quantity = 0 });
                }

          

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetCartItemCount(userId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }





        public async Task<int> RemoveItem(int productId)
        {
            string userId = GetUserrId();
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("User is not logged in");

                var cart = await GetUserCart(userId);
                if (cart is null)
                    throw new Exception("Invalid cart");

                var cartItem = _db.CartDetails
                                  .FirstOrDefault(a => a.ShoppingcartId == cart.Id && a.ProductId == productId);

                if (cartItem is null)
                    throw new Exception("No items in cart");
                else if (cartItem.Quanntity == 1)
                    _db.CartDetails.Remove(cartItem);
                else
                    cartItem.Quanntity = cartItem.Quanntity - 1;

                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                
            }
            var cartItemCount = await GetCartItemCount(userId);
            return cartItemCount;



        }
        public async Task<Shoppingcart> GetUserCart(string userId)
        {
            return await _db.Shoppingcarts
                .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.Product)
                .ThenInclude(p => p.ItemType) 
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }




        public async Task<Shoppingcart> GetUserCart()
        {
            var userId = GetUserrId(); 
            if (string.IsNullOrEmpty(userId))
                throw new Exception("User is not logged in");

            var cart = await _db.Shoppingcarts
                .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return cart;
        }


        public async Task<int> GetCartItemCount(string userId = "")
        {
            if (string.IsNullOrEmpty(userId))
            {
                userId = GetUserrId();
            }

            var count = await (from cart in _db.Shoppingcarts
                               join cartDetail in _db.CartDetails
                               on cart.Id equals cartDetail.ShoppingcartId
                               where cart.UserId == userId
                               select cartDetail.Quanntity
                              ).SumAsync();

            return count;
        }

        public async Task<Order> DoCheckout(CheckoutModel model)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var userId = GetUserrId();
                if (string.IsNullOrEmpty(userId))
                    throw new UnauthorizedAccessException("User is not logged-in");

                var cart = await _db.Shoppingcarts
                    .Include(c => c.CartDetails)
                    .ThenInclude(cd => cd.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                    throw new InvalidOperationException("Invalid cart");

                if (!cart.CartDetails.Any())
                    throw new InvalidOperationException("Cart is empty");

                var pendingRecord = await _db.orderStatuses
                    .FirstOrDefaultAsync(s => s.StatusName == "Pending");
                if (pendingRecord == null)
                    throw new InvalidOperationException("Order status does not have 'Pending' status");

                var order = new Order
                {
                    UserId = userId,
                    CreateDate = DateTime.UtcNow,
                    Name = model.Name,
                    Email = model.Email,
                    MobileNumber = model.MobileNumber,
                    OrderStatusId = pendingRecord.Id,
                    Address = model.Address,
                    PaymentMethod = model.PaymentMethod,
                    IsDeleted = false,
                    IsPaid = false
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                foreach (var item in cart.CartDetails)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quanntity,
                        UnitPrice = item.UnitPrice
                    };
                    _db.OrderDetails.Add(orderDetail);
                }

                _db.CartDetails.RemoveRange(cart.CartDetails);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return order; 
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Checkout failed: {ex.InnerException?.Message ?? ex.Message}");

            }
        }

        public async Task<Order?> GetOrderById(int orderId)
        {
            return await _db.Orders
                .Include(o => o.OrderDetail)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }




        private string GetUserrId()
        {
            var principal = _httpcontextAccessor.HttpContext.User;
            string userId = _userManager.GetUserId(principal);
            return userId;
        }

    }
} 
