
using LetdsGoAndDive.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IInventoryRepository
{
    Task<IEnumerable<StockDisplayModel>> GetStockSummary(string search = "");
    Task<IEnumerable<SalesByProductDto>> GetSalesByProduct(DateTime? from = null, DateTime? to = null, int? itemTypeId = null);
    Task<IEnumerable<StockMovementDto>> GetStockMovement(DateTime? from = null, DateTime? to = null, int? productId = null);
    Task<IEnumerable<Product>> GetProducts();
}
