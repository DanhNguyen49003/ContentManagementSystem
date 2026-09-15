using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ContentManagementSystem.ApplicationCore.Entities.Identity;

namespace ContentManagementSystem.Seeders
{
    public static class IdentityDataSeeder
    {
        public static readonly string[] Roles = new[]
        {
            "Admin",
            "QA Manager",
            "QA Coordinator",
            "Customer"
        };

        public static async Task SeedAsync(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            var roleManager = services.GetRequiredService<RoleManager<ContentRole>>();
            var userManager = services.GetRequiredService<UserManager<ContentUser>>();

            // 1. Seed Roles
            foreach (var roleName in Roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ContentRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpper(),
                        Avatar = $"role-{roleName.ToLower().Replace(" ", "-")}"
                    });
                }
            }

            // 2. Seed Default Users for each of the 4 roles
            await SeedUserAsync(userManager, "admin@cms.com", "Admin@123", "Quản trị viên", "Admin");
            await SeedUserAsync(userManager, "qamanager@cms.com", "Manager@123", "Trưởng ban QA", "QA Manager");
            await SeedUserAsync(userManager, "qacoordinator@cms.com", "Coord@123", "Điều phối viên QA", "QA Coordinator");
            await SeedUserAsync(userManager, "customer@cms.com", "Customer@123", "Khách hàng", "Customer");
        }

        private static async Task SeedUserAsync(
            UserManager<ContentUser> userManager,
            string email,
            string password,
            string fullName,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ContentUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = fullName,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
            else
            {
                // Ensure role is assigned if not already
                if (!await userManager.IsInRoleAsync(user, role))
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }
}

