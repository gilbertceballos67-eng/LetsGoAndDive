using Microsoft.AspNetCore.Mvc;
using LetdsGoAndDive.Models;
using LetdsGoAndDive.Repositories;
using System.Threading.Tasks;
using X.PagedList;
using X.PagedList.Extensions;

namespace LetdsGoAndDive.Controllers
{
    public class ShopController : Controller
    {
        private readonly IHomeRepository _homeRepo;

        public ShopController(IHomeRepository homeRepo)
        {
            _homeRepo = homeRepo;
        }

        public async Task<IActionResult> Index(string sTerm = "", int itemtypeId = 0, int page = 1)
        {
            var products = await _homeRepo.GetProduct(sTerm, itemtypeId);
            var itemTypes = await _homeRepo.ItemTypes();
            var pagedProducts = products.ToPagedList(page, 16);

            var model = new HomeIndexModel
            {
                PagedProducts = pagedProducts,
                ItemTypes = itemTypes,
                STerm = sTerm,
                ItemTypeId = itemtypeId
            };

            return View(model);
        }
    }
}
