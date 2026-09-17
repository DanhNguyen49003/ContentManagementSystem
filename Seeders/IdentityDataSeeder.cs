using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ContentManagementSystem.ApplicationCore.Entities.Identity;
using ContentManagementSystem.DataLayer;

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

            // 2. Seed Default Departments
            var contentDb = services.GetService<ContentManageDbContext>();
            Guid itDepartmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            if (contentDb != null)
            {
                try
                {
                    // Tự động bảo đảm cấu trúc bảng Departments và các cột DepartmentId trên Supabase
                    await contentDb.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS ""Departments"" (
                            ""Id"" uuid NOT NULL PRIMARY KEY,
                            ""Name"" character varying(150) NOT NULL,
                            ""Description"" character varying(500),
                            ""CreatedAt"" timestamp with time zone NOT NULL,
                            ""IsDeleted"" boolean NOT NULL DEFAULT false,
                            ""DeletedAt"" timestamp with time zone
                        );
                        CREATE TABLE IF NOT EXISTS ""SubmissionWindows"" (
                            ""Id"" uuid NOT NULL PRIMARY KEY,
                            ""Name"" character varying(150) NOT NULL,
                            ""Description"" character varying(500),
                            ""StartDate"" timestamp with time zone NOT NULL,
                            ""ClosureDate"" timestamp with time zone NOT NULL,
                            ""FinalClosureDate"" timestamp with time zone NOT NULL,
                            ""IsActive"" boolean NOT NULL DEFAULT true,
                            ""CreatedAt"" timestamp with time zone NOT NULL,
                            ""IsDeleted"" boolean NOT NULL DEFAULT false,
                            ""DeletedAt"" timestamp with time zone
                        );
                        ALTER TABLE ""AspNetUsers"" ADD COLUMN IF NOT EXISTS ""DepartmentId"" uuid;
                        ALTER TABLE ""Posts"" ADD COLUMN IF NOT EXISTS ""DepartmentId"" uuid;
                        ALTER TABLE ""Posts"" ADD COLUMN IF NOT EXISTS ""IsAnonymous"" boolean DEFAULT false;
                        ALTER TABLE ""Posts"" ADD COLUMN IF NOT EXISTS ""Status"" character varying(50) DEFAULT 'Pending';
                        ALTER TABLE ""Posts"" ADD COLUMN IF NOT EXISTS ""ReviewerFeedback"" character varying(1000);
                        ALTER TABLE ""Posts"" ADD COLUMN IF NOT EXISTS ""ReviewedAt"" timestamp with time zone;
                        ALTER TABLE ""Posts"" ADD COLUMN IF NOT EXISTS ""ReviewedById"" text;
                        ALTER TABLE ""Posts"" ADD COLUMN IF NOT EXISTS ""SubmissionWindowId"" uuid;
                    ");

                    var itDept = await contentDb.Departments.FirstOrDefaultAsync(d => d.Name == "Khoa Công nghệ thông tin");
                    if (itDept == null)
                    {
                        itDept = new ContentManagementSystem.ApplicationCore.Entities.Department
                        {
                            Id = itDepartmentId,
                            Name = "Khoa Công nghệ thông tin",
                            Description = "Khoa Công nghệ thông tin & Đổi mới sáng tạo số",
                            CreatedAt = DateTime.UtcNow
                        };
                        contentDb.Departments.Add(itDept);
                    }
                    itDepartmentId = itDept.Id;

                    if (!await contentDb.Departments.AnyAsync(d => d.Name == "Khoa Quản trị kinh doanh"))
                    {
                        contentDb.Departments.Add(new ContentManagementSystem.ApplicationCore.Entities.Department
                        {
                            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                            Name = "Khoa Quản trị kinh doanh",
                            Description = "Khoa Quản trị kinh doanh & Tài chính số",
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    if (!await contentDb.Departments.AnyAsync(d => d.Name == "Phòng Đào tạo & Khảo thí"))
                    {
                        contentDb.Departments.Add(new ContentManagementSystem.ApplicationCore.Entities.Department
                        {
                            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                            Name = "Phòng Đào tạo & Khảo thí",
                            Description = "Phòng Quản lý Đào tạo, Khảo thí và Đảm bảo chất lượng",
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await contentDb.SaveChangesAsync();

                    // Tự động gán phòng ban cho tất cả bài viết chưa có DepartmentId
                    await contentDb.Database.ExecuteSqlRawAsync($@"
                        UPDATE ""Posts"" SET ""DepartmentId"" = '{itDepartmentId}' WHERE ""DepartmentId"" IS NULL;
                    ");

                    // Tự động gán phòng ban mặc định cho tất cả user (ngoại trừ Admin) nếu chưa có DepartmentId
                    await contentDb.Database.ExecuteSqlRawAsync($@"
                        UPDATE ""AspNetUsers"" 
                        SET ""DepartmentId"" = '{itDepartmentId}' 
                        WHERE ""DepartmentId"" IS NULL 
                          AND ""Id"" NOT IN (
                              SELECT ur.""UserId"" FROM ""AspNetUserRoles"" ur 
                              JOIN ""AspNetRoles"" r ON ur.""RoleId"" = r.""Id"" 
                              WHERE UPPER(r.""Name"") = 'ADMIN' OR UPPER(r.""NormalizedName"") = 'ADMIN'
                          );
                    ");

                    // 2b. Seed Default Submission Window
                    var defaultWindow = await contentDb.SubmissionWindows.FirstOrDefaultAsync(w => !w.IsDeleted);
                    if (defaultWindow == null)
                    {
                        defaultWindow = new ContentManagementSystem.ApplicationCore.Entities.SubmissionWindow
                        {
                            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                            Name = "Đợt Đóng góp & Nộp Bài viết - Học kỳ Hiện tại",
                            Description = "Đợt tiếp nhận bài viết, ý tưởng nghiên cứu và sáng kiến cải tiến cho cộng đồng học thuật.",
                            StartDate = DateTime.UtcNow.AddDays(-30),
                            ClosureDate = DateTime.UtcNow.AddDays(60),
                            FinalClosureDate = DateTime.UtcNow.AddDays(90),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        contentDb.SubmissionWindows.Add(defaultWindow);
                        await contentDb.SaveChangesAsync();
                    }

                    // Tự động gán SubmissionWindow và chuẩn hóa trạng thái Status cho các bài viết hiện tại
                    await contentDb.Database.ExecuteSqlRawAsync($@"
                        UPDATE ""Posts"" SET ""SubmissionWindowId"" = '{defaultWindow.Id}' WHERE ""SubmissionWindowId"" IS NULL;
                        UPDATE ""Posts"" SET ""Status"" = 'Approved' WHERE ""IsPublished"" = true AND (""Status"" IS NULL OR ""Status"" = 'Pending');
                        UPDATE ""Posts"" SET ""Status"" = 'Pending' WHERE ""IsPublished"" = false AND ""Status"" IS NULL;
                    ");
                }
                catch
                {
                    // Tránh crash nếu cơ sở dữ liệu đã có sẵn
                }
            }

            // 3. Seed Default Users for each of the 4 roles
            // Admin không cần phòng ban (toàn quyền trên tất cả phòng ban)
            await SeedUserAsync(userManager, "admin@cms.com", "Admin@123", "Quản trị viên", "Admin", null);
            await SeedUserAsync(userManager, "qamanager@cms.com", "Manager@123", "Trưởng ban QA", "QA Manager", null);
            await SeedUserAsync(userManager, "qacoordinator@cms.com", "Coord@123", "Điều phối viên QA (Khoa CNTT)", "QA Coordinator", itDepartmentId);
            await SeedUserAsync(userManager, "customer@cms.com", "Customer@123", "Khách hàng", "Customer", itDepartmentId);

            // 4. Chuẩn hóa tên chuyên mục có dấu tiếng Việt chuẩn
            if (contentDb != null)
            {
                var congNghe = await contentDb.Categories.FirstOrDefaultAsync(c => c.Name == "Cong Nghe");
                if (congNghe != null)
                {
                    congNghe.Name = "Công nghệ";
                    congNghe.Description = "Chuyên mục tin tức công nghệ, đời sống số";
                    await contentDb.SaveChangesAsync();
                }
            }
        }

        private static async Task SeedUserAsync(
            UserManager<ContentUser> userManager,
            string email,
            string password,
            string fullName,
            string role,
            Guid? departmentId = null)
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
                    DepartmentId = departmentId,
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
                // Đồng bộ mật khẩu đúng với nút bấm gợi ý (Admin@123, Customer@123...) nếu bị lệch
                var isPasswordValid = await userManager.CheckPasswordAsync(user, password);
                if (!isPasswordValid)
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    await userManager.ResetPasswordAsync(user, token, password);
                }

                // Đảm bảo tài khoản đã kích hoạt, không bị khóa
                bool needUpdate = false;
                if (role == "Admin")
                {
                    // Admin luôn có toàn quyền, không gắn vào phòng ban cố định nào
                    if (user.DepartmentId != null)
                    {
                        user.DepartmentId = null;
                        needUpdate = true;
                    }
                }
                else if (departmentId != null && user.DepartmentId != departmentId)
                {
                    user.DepartmentId = departmentId;
                    needUpdate = true;
                }
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    needUpdate = true;
                }
                if (user.LockoutEnd != null)
                {
                    user.LockoutEnd = null;
                    needUpdate = true;
                }
                if (string.IsNullOrWhiteSpace(user.FullName))
                {
                    user.FullName = fullName;
                    needUpdate = true;
                }

                if (needUpdate)
                {
                    await userManager.UpdateAsync(user);
                }

                // Đảm bảo role chuẩn
                if (!await userManager.IsInRoleAsync(user, role))
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }
}

