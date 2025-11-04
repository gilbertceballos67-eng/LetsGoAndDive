using LetdsGoAndDive.Data;
using LetdsGoAndDive.Models;
using Microsoft.EntityFrameworkCore;

namespace LetdsGoAndDive.Repositories
{
    public class ItemTypeRepository : IItemTypeRepository
    {
        private readonly ApplicationDbContext _context;

        public ItemTypeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddItemType(ItemType itemType)
        {
            _context.ItemTypes.Add(itemType);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateItemType(ItemType itemType)
        {
            _context.ItemTypes.Update(itemType);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteItemType(ItemType itemType)
        {
            _context.ItemTypes.Remove(itemType);
            await _context.SaveChangesAsync();
        }

        public async Task<ItemType?> GetItemTypeById(int id)
        {
            return await _context.ItemTypes.FindAsync(id);
        }

        public async Task<IEnumerable<ItemType>> GetItemTypes()
        {
            return await _context.ItemTypes.ToListAsync();
        }
    }

    public interface IItemTypeRepository
    {
        Task AddItemType(ItemType itemType);
        Task UpdateItemType(ItemType itemType);
        Task<ItemType?> GetItemTypeById(int id);
        Task DeleteItemType(ItemType itemType);
        Task<IEnumerable<ItemType>> GetItemTypes();
    }
}
