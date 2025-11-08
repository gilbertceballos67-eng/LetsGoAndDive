using Microsoft.AspNetCore.Mvc;
using LetdsGoAndDive.Models;
using LetdsGoAndDive.Repositories;
using System.Threading.Tasks;
using X.PagedList;
using System.Drawing.Printing;
using X.PagedList.Extensions;
using X.PagedList;


namespace LetdsGoAndDive.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeRepository _homeRepo;

        public HomeController(IHomeRepository homeRepo)
        {
            _homeRepo = homeRepo;
        }

        public async Task<IActionResult> Index(string sTerm = "", int itemtypeId = 0, int page = 1)
        {
            var products = await _homeRepo.GetProduct(sTerm, itemtypeId);
            var itemTypes = await _homeRepo.ItemTypes();

            
            var pagedProducts = products.ToPagedList(page, 10);

            var model = new HomeIndexModel
            {
                PagedProducts = pagedProducts,
                ItemTypes = itemTypes,
                STerm = sTerm,
                ItemTypeId = itemtypeId
            };

            return View(model);
        }




        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }
    }
}