using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using X.PagedList.Extensions;

namespace LetdsGoAndDive.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class StockController : Controller
    {
        private readonly IStockRepository _stockRepo;

        public StockController(IStockRepository stockRepo)
        {
            _stockRepo = stockRepo;
        }

        public async Task<IActionResult> Index(string sTerm = "", int page = 1, int pageSize = 10)
        {
            var stocks = await _stockRepo.GetStocks(sTerm);

            
            var pagedStocks = stocks.ToPagedList(page, pageSize);

            ViewBag.SearchTerm = sTerm;

            return View(pagedStocks);
        }

        // GET
        public async Task<IActionResult> ManageStock(int productId)
        {
            var existingStock = await _stockRepo.GetStockByProductId(productId);
            var stock = new StockDTO
            {
                ProductId = productId,
                Quantity = existingStock != null ? existingStock.Quantity : 0
            };
            return View(stock);
        }

        // POST
        [HttpPost]
        public async Task<IActionResult> ManageStock(StockDTO stock)
        {
            if (!ModelState.IsValid)
                return View(stock);

            try
            {
                await _stockRepo.ManageStock(stock);
                TempData["successMessage"] = "Stock updated successfully.";
            }
            catch
            {
                TempData["errorMessage"] = "Something went wrong!";
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
