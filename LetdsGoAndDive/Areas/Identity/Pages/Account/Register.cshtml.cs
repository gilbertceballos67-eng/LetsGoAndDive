using System.ComponentModel.DataAnnotations;
using LetdsGoAndDive.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LetdsGoAndDive.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required, EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            [Required]
            [Display(Name = "Mobile Number")]
            public string MobileNumber { get; set; }

            [Required]
            [Display(Name = "Address")]
            public string Address { get; set; }
        }

        // accept returnUrl so model binder won't create a problematic entry; then remove it from ModelState
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            // ensure returnUrl has a sensible default
            returnUrl ??= Url.Content("~/");

            // If model binder put returnUrl into ModelState (and it's causing "required" failure),
            // remove it so validation of the Input model can proceed normally.
            if (ModelState.ContainsKey("returnUrl"))
                ModelState.Remove("returnUrl");
            if (ModelState.ContainsKey("ReturnUrl"))
                ModelState.Remove("ReturnUrl");

            // debug: log any validation errors (helpful while testing)
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state.Errors.Count > 0)
                {
                    _logger.LogWarning("Validation Error on {Key}: {Errors}", key, string.Join(" | ", state.Errors.Select(e => e.ErrorMessage)));
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed. Check required fields.");
                TempData["RegisterError"] = "Please fill in all required fields correctly.";
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FullName = Input.FullName,
                MobileNumber = Input.MobileNumber,
                Address = Input.Address,
                EmailConfirmed = true
            };

            _logger.LogInformation("Attempting to create user: {Email}", Input.Email);
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created successfully: {Email}", Input.Email);
                TempData["RegisterSuccess"] = "Registration complete! You are now signed in.";

                await _signInManager.SignInAsync(user, isPersistent: false);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return LocalRedirect(Url.Content("~/") + "?registered=true");
            }

            foreach (var error in result.Errors)
            {
                _logger.LogError("User creation failed: {Error}", error.Description);
                ModelState.AddModelError(string.Empty, error.Description);
            }

            TempData["RegisterError"] = "Registration failed. Please check your details and try again.";
            return Page();
        }
    }
}
