namespace LetdsGoAndDive.Repositories
{
    public interface IHomeRepository
    {
        Task<IEnumerable<Product>> GetProduct(string sTerm = "", int itemtypeId = 0);
        Task<IEnumerable<ItemType>> ItemTypes();
    }
}
