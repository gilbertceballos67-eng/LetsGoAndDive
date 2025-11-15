using LetdsGoAndDive.Data;
using LetdsGoAndDive.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using X.PagedList.Extensions;

namespace LetdsGoAndDive.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult ManageUsers(string searchTerm, string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                users = users.Where(u =>
                    u.FullName.Contains(searchTerm) ||
                    (u.Email != null && u.Email.Contains(searchTerm)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)));
            }

            users = sortOrder == "desc"
                ? users.OrderByDescending(u => u.FullName)
                : users.OrderBy(u => u.FullName);

            var pagedUsers = users
                .Select(u => new ApplicationUser
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    MobileNumber = u.MobileNumber ?? "N/A",
                    Address = u.Address
                })
                .ToPagedList(page, pageSize);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortOrder = sortOrder;

            return View(pagedUsers);
        }

        [HttpGet]
        public IActionResult AddUser() => View();

        [HttpPost]
        public async Task<IActionResult> AddUser(ApplicationUser model, string password)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Basic password length check
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError("", "Password must be at least 6 characters long.");
                return View(model);
            }

            // Prevent duplicate email
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "This email is already registered.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email, 
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                MobileNumber = model.MobileNumber,
                Address = model.Address,
                EmailConfirmed = true 
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Ensure default role exists
                if (!await _roleManager.RoleExistsAsync("User"))
                    await _roleManager.CreateAsync(new IdentityRole("User"));

                await _userManager.AddToRoleAsync(user, "User");

                TempData["success"] = "User added successfully!";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Show identity errors
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"❌ {error.Code}: {error.Description}");
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(ApplicationUser model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.MobileNumber = model.MobileNumber;
            user.Address = model.Address;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["success"] = "User updated successfully!";
                return RedirectToAction(nameof(ManageUsers));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            TempData["success"] = "User deleted successfully!";
            return RedirectToAction(nameof(ManageUsers));
        }
    }
}
