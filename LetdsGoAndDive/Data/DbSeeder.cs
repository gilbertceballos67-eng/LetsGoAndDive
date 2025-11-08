using Microsoft.AspNetCore.Identity;
using LetdsGoAndDive.Constants;
using LetdsGoAndDive.Models;
using LetdsGoAndDive.Data; 

namespace LetdsGoAndDive.Data
{
    public class DbSeeder
    {
        public static async Task SeedDefaultData(IServiceProvider service)
        {
            var userMgr = service.GetRequiredService<UserManager<ApplicationUser>>();
            var roleMgr = service.GetRequiredService<RoleManager<IdentityRole>>();
            var context = service.GetRequiredService<ApplicationDbContext>(); 

           
            if (!await roleMgr.RoleExistsAsync(Roles.Admin.ToString()))
                await roleMgr.CreateAsync(new IdentityRole(Roles.Admin.ToString()));

            if (!await roleMgr.RoleExistsAsync(Roles.User.ToString()))
                await roleMgr.CreateAsync(new IdentityRole(Roles.User.ToString()));

           
            var adminEmail = "admin@gmail.com";
            var adminUser = await userMgr.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    FullName = "Administrator",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    MobileNumber = "09276797496",
                    Address = "CAVITE, Philippines"
                };

                var createResult = await userMgr.CreateAsync(admin, "Admin@123");
                if (createResult.Succeeded)
                {
                    await userMgr.AddToRoleAsync(admin, Roles.Admin.ToString());
                }
                else
                {
                    throw new Exception("Failed to create admin user: " +
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }

          
            if (!context.orderStatuses.Any())
            {
                context.orderStatuses.AddRange(
                    new OrderStatus { StatusId = 1, StatusName = "Pending" },
                    
                    new OrderStatus { StatusId = 2, StatusName = "Shipped" },
                    new OrderStatus { StatusId = 3, StatusName = "Delivered" },
                    new OrderStatus { StatusId = 4, StatusName = "Cancelled" }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}