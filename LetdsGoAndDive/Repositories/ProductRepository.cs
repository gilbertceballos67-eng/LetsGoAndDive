using LetdsGoAndDive.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LetdsGoAndDive.Repositories
{
    public interface IProductRepository
    {
        Task AddProduct(Product product);
        Task UpdateProduct(Product product);
        Task<Product?> GetProductById(int id);
        Task<IEnumerable<Product>> GetProducts();
        Task ArchiveProduct(Product product);
        Task<IEnumerable<Product>> GetActiveProducts();
        Task<IEnumerable<Product>> GetArchivedProducts();


    }

    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateProduct(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task ArchiveProduct(Product product)
        {
            product.IsArchived = true;
            await _context.SaveChangesAsync();
        }


        public async Task<Product?> GetProductById(int id)
        {
       
            return await _context.Products
                .Include(p => p.ItemType)
                .FirstOrDefaultAsync(p => p.Id == id);



        }

        public async Task<IEnumerable<Product>> GetProducts()
        {
            return await _context.Products
                                 .Include(p => p.ItemType)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetActiveProducts()
        {
            return await _context.Products
                .Where(p => !p.IsArchived)
                .Include(p => p.ItemType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetArchivedProducts()
        {
            return await _context.Products
                .Where(p => p.IsArchived)
                .Include(p => p.ItemType)
                .ToListAsync();
        }

    }
}
