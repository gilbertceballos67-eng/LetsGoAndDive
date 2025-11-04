using LetdsGoAndDive.Models;
using LetdsGoAndDive.Models.DTOs;
using LetdsGoAndDive.Repositories;
using LetdsGoAndDive.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LetdsGoAndDive.Controllers
{
    [Authorize(Roles = nameof(Constants.Roles.Admin))]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly IItemTypeRepository _itemTypeRepo;
        private readonly IFileService _fileService;

        public ProductController(IProductRepository productRepo, IItemTypeRepository itemTypeRepo, IFileService fileService)
        {
            _productRepo = productRepo;
            _itemTypeRepo = itemTypeRepo;
            _fileService = fileService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productRepo.GetProducts();
            return View(products);
        }

        public async Task<IActionResult> AddProduct()
        {
            var itemTypeSelectList = (await _itemTypeRepo.GetItemTypes()).Select(item => new SelectListItem
            {
                Text = item.ItemTypeName,
                Value = item.Id.ToString(),
            });

            ProductDTO productToAdd = new() { ItemTypeList = itemTypeSelectList };
            return View(productToAdd);
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(ProductDTO productToAdd)
        {
            var itemTypeSelectList = (await _itemTypeRepo.GetItemTypes()).Select(item => new SelectListItem
            {
                Text = item.ItemTypeName,
                Value = item.Id.ToString(),
            });
            productToAdd.ItemTypeList = itemTypeSelectList;

            if (!ModelState.IsValid)
                return View(productToAdd);

            try
            {
                if (productToAdd.ImageFile != null)
                {
                    if (productToAdd.ImageFile.Length > 1 * 1024 * 1024)
                    {
                        throw new InvalidOperationException("Image file cannot exceed 1 MB");
                    }
                    string[] allowedExtensions = new string[] { ".jpeg", ".jpg", ".png" };
                    string imageName = await _fileService.SaveFile(productToAdd.ImageFile, allowedExtensions);
                    productToAdd.Image = imageName;
                }

                Product product = new()
                {
                    Id = productToAdd.Id,
                    ProductName = productToAdd.ProductName,
                    Image = productToAdd.Image,
                    ItemTypeID = productToAdd.ItemTypeID,
                    Price = productToAdd.Price
                };

                await _productRepo.AddProduct(product);
                TempData["successMessage"] = "Product added successfully!";
                return RedirectToAction(nameof(AddProduct));
            }
            catch (InvalidOperationException ex)
            {
                TempData["errorMessage"] = ex.Message;
                return View(productToAdd);
            }
            catch (FileNotFoundException ex)
            {
                TempData["errorMessage"] = ex.Message;
                return View(productToAdd);
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "Error saving product.";
                return View(productToAdd);
            }
        }

        public async Task<IActionResult> UpdateProduct(int id)
        {
            var product = await _productRepo.GetProductById(id);
            if (product == null)
            {
                TempData["errorMessage"] = $"Product with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            var itemTypeSelectList = (await _itemTypeRepo.GetItemTypes()).Select(item => new SelectListItem
            {
                Text = item.ItemTypeName,
                Value = item.Id.ToString(),
                Selected = item.Id == product.ItemTypeID
            });

            ProductDTO productToUpdate = new()
            {
                ItemTypeList = itemTypeSelectList,
                ProductName = product.ProductName,
                ItemTypeID = product.ItemTypeID,
                Price = product.Price,
                Image = product.Image
            };

            return View(productToUpdate);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProduct(ProductDTO productToUpdate)
        {
            var itemTypeSelectList = (await _itemTypeRepo.GetItemTypes()).Select(item => new SelectListItem
            {
                Text = item.ItemTypeName,
                Value = item.Id.ToString(),
                Selected = item.Id == productToUpdate.ItemTypeID
            });
            productToUpdate.ItemTypeList = itemTypeSelectList;

            if (!ModelState.IsValid)
                return View(productToUpdate);

            try
            {
                string oldImage = "";
                if (productToUpdate.ImageFile != null)
                {
                    if (productToUpdate.ImageFile.Length > 1 * 1024 * 1024)
                    {
                        throw new InvalidOperationException("Image file cannot exceed 1 MB");
                    }
                    string[] allowedExtensions = new string[] { ".jpeg", ".jpg", ".png" };
                    string imageName = await _fileService.SaveFile(productToUpdate.ImageFile, allowedExtensions);
                    oldImage = productToUpdate.Image;
                    productToUpdate.Image = imageName;
                }

                Product product = new()
                {
                    Id = productToUpdate.Id,
                    ProductName = productToUpdate.ProductName,
                    ItemTypeID = productToUpdate.ItemTypeID,
                    Price = productToUpdate.Price,
                    Image = productToUpdate.Image
                };

                await _productRepo.UpdateProduct(product);

                if (!string.IsNullOrWhiteSpace(oldImage))
                {
                    _fileService.DeleteFile(oldImage);
                }

                TempData["successMessage"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["errorMessage"] = ex.Message;
                return View(productToUpdate);
            }
            catch (FileNotFoundException ex)
            {
                TempData["errorMessage"] = ex.Message;
                return View(productToUpdate);
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "Error saving product.";
                return View(productToUpdate);
            }
        }

        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _productRepo.GetProductById(id);
                if (product == null)
                {
                    TempData["errorMessage"] = $"Product with ID {id} not found.";
                }
                else
                {
                    await _productRepo.DeleteProduct(product);
                    if (!string.IsNullOrWhiteSpace(product.Image))
                    {
                        _fileService.DeleteFile(product.Image);
                    }
                    TempData["successMessage"] = "Product deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
