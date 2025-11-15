// Repositories/InventoryRepository.cs
using LetdsGoAndDive.Data;
using LetdsGoAndDive.Models;
using LetdsGoAndDive.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class InventoryRepository : IInventoryRepository
{
    private readonly ApplicationDbContext _db;
    public InventoryRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    // Basic stock summary used for table + pie chart
    public async Task<IEnumerable<StockDisplayModel>> GetStockSummary(string search = "")
    {
        var q = from p in _db.Products
                join s in _db.Stocks on p.Id equals s.ProductId into ps
                from stock in ps.DefaultIfEmpty()
                where string.IsNullOrWhiteSpace(search) || p.ProductName.ToLower().Contains(search.ToLower())
                select new StockDisplayModel
                {
                    ProductId = p.Id,
                    ProductName = p.ProductName,
                    Quantity = stock == null ? 0 : stock.Quantity
                };

        return await q.ToListAsync();
    }

    // Sales by product (only count orders that are paid)
    public async Task<IEnumerable<SalesByProductDto>> GetSalesByProduct(DateTime? from = null, DateTime? to = null, int? itemTypeId = null)
    {
        var orders = _db.Orders.AsQueryable()
            .Where(o => o.IsPaid); // treat paid orders as confirmed sales

        if (from.HasValue) orders = orders.Where(o => o.CreateDate >= from.Value);
        if (to.HasValue) orders = orders.Where(o => o.CreateDate <= to.Value);

        var q = from od in _db.OrderDetails
                join o in orders on od.OrderId equals o.Id
                join p in _db.Products on od.ProductId equals p.Id
                where !itemTypeId.HasValue || p.ItemTypeID == itemTypeId.Value
                group od by new { od.ProductId, p.ProductName } into g
                select new SalesByProductDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                };

        return await q.OrderByDescending(x => x.QuantitySold).ToListAsync();
    }

    // Stock movement (simplified): sold per day in range
    public async Task<IEnumerable<StockMovementDto>> GetStockMovement(DateTime? from = null, DateTime? to = null, int? productId = null)
    {
        var orders = _db.Orders.AsQueryable().Where(o => o.IsPaid);
        if (from.HasValue) orders = orders.Where(o => o.CreateDate >= from.Value);
        if (to.HasValue) orders = orders.Where(o => o.CreateDate <= to.Value);

        var q = from od in _db.OrderDetails
                join o in orders on od.OrderId equals o.Id
                join p in _db.Products on od.ProductId equals p.Id
                where !productId.HasValue || od.ProductId == productId.Value
                group new { od, o } by new { Date = o.CreateDate.Date, od.ProductId, p.ProductName } into g
                select new StockMovementDto
                {
                    Date = g.Key.Date,
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(x => x.od.Quantity)
                };

        return await q.OrderBy(x => x.Date).ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProducts()
    {
        return await _db.Products.ToListAsync();
    }
}
