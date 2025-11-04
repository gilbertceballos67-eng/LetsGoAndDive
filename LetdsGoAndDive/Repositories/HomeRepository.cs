using Microsoft.EntityFrameworkCore;

namespace LetdsGoAndDive.Repositories
{
    public class HomeRepository : IHomeRepository
    {
        private readonly ApplicationDbContext _db;

        public HomeRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ItemType>> ItemTypes()
        {
            return await _db.ItemTypes.ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProduct(string sTerm = "", int itemtypeId = 0)
        {
            sTerm = sTerm?.ToLower() ?? "";

           
            var query = _db.Products
                .Include(p => p.ItemType)  
                .Include(p => p.Stock)     
                .AsQueryable();

           
            if (!string.IsNullOrWhiteSpace(sTerm))
            {
                query = query.Where(p => p.ProductName.ToLower().Contains(sTerm));
            }

           
            if (itemtypeId > 0)
            {
                query = query.Where(p => p.ItemTypeID == itemtypeId);
            }

          
            var products = await query.AsNoTracking().ToListAsync();

            
            foreach (var product in products)
            {
                product.Quantity = product.Stock?.Quantity ?? 0;
            }

            return products;
        }
    }
}