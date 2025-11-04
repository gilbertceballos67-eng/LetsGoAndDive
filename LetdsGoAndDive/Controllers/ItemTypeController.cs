using LetdsGoAndDive.Models;
using LetdsGoAndDive.Models.DTOs;
using LetdsGoAndDive.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LetdsGoAndDive.Controllers
{
    [Authorize(Roles = "Admin")] // Only Admins can manage Item Types
    public class ItemTypeController : Controller
    {
        private readonly IItemTypeRepository _itemTypeRepo;

        public ItemTypeController(IItemTypeRepository itemTypeRepo)
        {
            _itemTypeRepo = itemTypeRepo;
        }

        // GET: ItemType/Index
        public async Task<IActionResult> Index()
        {
            var itemTypes = await _itemTypeRepo.GetItemTypes();
            return View(itemTypes);
        }

        // GET: ItemType/AddItemType
        public IActionResult AddItemType()
        {
            return View();
        }

        // POST: ItemType/AddItemType
        [HttpPost]
        public async Task<IActionResult> AddItemType(ItemTypeDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                var newItemType = new ItemType
                {
                    ItemTypeName = dto.ItemTypeName
                };

                await _itemTypeRepo.AddItemType(newItemType);
                TempData["successMessage"] = "Item Type added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "Failed to add Item Type.";
                return View(dto);
            }
        }

        // GET: ItemType/UpdateItemType/5
        public async Task<IActionResult> UpdateItemType(int id)
        {
            var itemType = await _itemTypeRepo.GetItemTypeById(id);
            if (itemType == null)
                return NotFound();

            var dto = new ItemTypeDTO
            {
                Id = itemType.Id,
                ItemTypeName = itemType.ItemTypeName
            };

            return View(dto);
        }

        // POST: ItemType/UpdateItemType
        [HttpPost]
        public async Task<IActionResult> UpdateItemType(ItemTypeDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                var updatedItemType = new ItemType
                {
                    Id = dto.Id,
                    ItemTypeName = dto.ItemTypeName
                };

                await _itemTypeRepo.UpdateItemType(updatedItemType);
                TempData["successMessage"] = "Item Type updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "Failed to update Item Type.";
                return View(dto);
            }
        }

        // GET: ItemType/DeleteItemType/5
        public async Task<IActionResult> DeleteItemType(int id)
        {
            var itemType = await _itemTypeRepo.GetItemTypeById(id);
            if (itemType == null)
                return NotFound();

            await _itemTypeRepo.DeleteItemType(itemType);
            TempData["successMessage"] = "Item Type deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
