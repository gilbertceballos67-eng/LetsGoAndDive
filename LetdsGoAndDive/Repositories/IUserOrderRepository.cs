namespace LetdsGoAndDive.Repositories
{
    public interface IUserOrderRepository
    {
        Task<IEnumerable<Order>> UserOrders(bool getAll = false);
        Task ChangeOrderStatus(UpdateOrderStatusModel data);
        Task TogglePaymentStatus(int orderId);
        Task<Order?> GetOrderById(int id);
        Task<IEnumerable<OrderStatus>> GetOrderStatuses();
        Task<bool> DeleteOrder(int orderId);
        Task UpdateOrder(Order order);

        Task<int> PermanentlyDeleteOldOrders();



    }
}