namespace LetdsGoAndDive.Repositories
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using LetdsGoAndDive.Models;

    public interface ICartRepository
    {
        Task<int> AddItem(int productId, int qty);
        Task<int> RemoveItem(int productId);
        Task<Shoppingcart> GetUserCart();
        Task<int> GetCartItemCount(string userId = "");
        Task<Shoppingcart> GetUserCart(string userId);

        Task<Order> DoCheckout(CheckoutModel model);
        Task<Order?> GetOrderById(int orderId);

    }
}
