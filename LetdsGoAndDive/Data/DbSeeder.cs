using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using LetdsGoAndDive.Constants;

namespace LetdsGoAndDive.Data
{
    public class DbSeeder
    {
        public static async Task SeedDefaultData(IServiceProvider service)
        {
            var userMgr = service.GetRequiredService<UserManager<IdentityUser>>();
            var roleMgr = service.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure roles exist
            if (!await roleMgr.RoleExistsAsync(Roles.Admin.ToString()))
                await roleMgr.CreateAsync(new IdentityRole(Roles.Admin.ToString()));

            if (!await roleMgr.RoleExistsAsync(Roles.User.ToString()))
                await roleMgr.CreateAsync(new IdentityRole(Roles.User.ToString()));

            // Ensure admin user exists
            var adminEmail = "admin@gmail.com";
            var adminUser = await userMgr.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
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
        }
    }
}
