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
            "Editor",
            "Author",
            "Moderator",
            "Subscriber"
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
                        Avatar = $"role-{roleName.ToLower()}"
                    });
                }
            }

            // 2. Seed Default Users for each role
            await SeedUserAsync(userManager, "admin@cms.com", "Admin@123", "Quản trị viên", "Admin");
            await SeedUserAsync(userManager, "editor@cms.com", "Editor@123", "Biên tập viên", "Editor");
            await SeedUserAsync(userManager, "author@cms.com", "Author@123", "Tác giả bài viết", "Author");
            await SeedUserAsync(userManager, "moderator@cms.com", "Moderator@123", "Kiểm duyệt viên", "Moderator");
            await SeedUserAsync(userManager, "user@cms.com", "User@123", "Độc giả thành viên", "Subscriber");
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

