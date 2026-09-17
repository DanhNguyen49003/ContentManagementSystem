using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ContentManagementSystem.ApplicationCore.Entities.Identity;
using ContentManagementSystem.DataLayer;

namespace ContentManagementSystem.Services
{
    public class CurrentUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = "User";
        public string Avatar { get; set; } = string.Empty;
        public string RoleName { get; set; } = "Khách";
        public string RoleBadgeClass { get; set; } = "bg-gray-100 text-gray-700";
        public Guid? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsQAManager { get; set; }
        public bool IsQACoordinator { get; set; }
        public bool IsCustomer { get; set; }
        public bool IsAuthenticated { get; set; }

        public string GetAvatarOrDefault()
        {
            if (!string.IsNullOrWhiteSpace(Avatar))
            {
                return Avatar;
            }

            // Zero-network SVG fallback - loads in 0ms without waiting for foreign CDNs
            var initials = "U";
            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                initials = string.Join("", parts.Select(p => p[0])).ToUpper();
                if (initials.Length > 2) initials = initials.Substring(0, 2);
            }

            return $"data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='80' height='80' viewBox='0 0 80 80'><rect width='80' height='80' rx='20' fill='%232563eb'/><text x='50%' y='55%' dominant-baseline='middle' text-anchor='middle' fill='%23ffffff' font-family='sans-serif' font-size='28' font-weight='bold'>{initials}</text></svg>";
        }
    }

    public interface ICurrentUserService
    {
        Task<CurrentUserDto> GetCurrentUserAsync();
        void ClearCache(string userId);
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ContentUser> _userManager;
        private readonly IMemoryCache _cache;
        private readonly ContentManageDbContext _dbContext;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor,
            UserManager<ContentUser> userManager,
            IMemoryCache cache,
            ContentManageDbContext dbContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _cache = cache;
            _dbContext = dbContext;
        }

        public async Task<CurrentUserDto> GetCurrentUserAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null || httpContext.User?.Identity?.IsAuthenticated != true)
            {
                return new CurrentUserDto();
            }

            // 1. Check HttpContext.Items (cùng 1 lượt request HTTP giữa Layout và View -> 0ms tuyệt đối)
            if (httpContext.Items.TryGetValue("CURRENT_USER_CACHE", out var itemObj) && itemObj is CurrentUserDto cachedFromContext)
            {
                return cachedFromContext;
            }

            var principal = httpContext.User;
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? principal.Identity?.Name
                         ?? "anonymous";

            // 2. Check RAM MemoryCache (lưu trên RAM server 10 phút, không cần gọi query Supabase từ Nhật Bản)
            var cacheKey = $"CurrentUserProfile_{userId}";
            if (_cache.TryGetValue(cacheKey, out CurrentUserDto? cachedFromRam) && cachedFromRam != null)
            {
                httpContext.Items["CURRENT_USER_CACHE"] = cachedFromRam;
                return cachedFromRam;
            }

            // 3. Nếu chưa có trên cache thì mới query DB 1 lần duy nhất rồi nạp vào cache
            var dto = new CurrentUserDto { IsAuthenticated = true };
            ContentUser? user = null;
            try
            {
                user = await _userManager.GetUserAsync(principal);
            }
            catch
            {
                // Fallback nếu kết nối mạng tạm thời gián đoạn
            }

            if (user != null)
            {
                dto.Id = user.Id.ToString();
                dto.Email = user.Email ?? "";
                dto.DisplayName = !string.IsNullOrWhiteSpace(user.FullName)
                    ? user.FullName
                    : (!string.IsNullOrWhiteSpace(user.UserName) ? user.UserName : (user.Email ?? "User"));
                dto.Avatar = user.Avatar ?? "";

                var roles = await _userManager.GetRolesAsync(user);
                dto.IsAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase)
                              || string.Equals(user.Email, "admin@cms.com", StringComparison.OrdinalIgnoreCase)
                              || principal.IsInRole("Admin");
                dto.IsQAManager = roles.Contains("QA Manager", StringComparer.OrdinalIgnoreCase) || principal.IsInRole("QA Manager");
                dto.IsQACoordinator = roles.Contains("QA Coordinator", StringComparer.OrdinalIgnoreCase) || principal.IsInRole("QA Coordinator");
                dto.IsCustomer = roles.Contains("Customer", StringComparer.OrdinalIgnoreCase) || principal.IsInRole("Customer");

                dto.DepartmentId = user.DepartmentId;
                if (dto.IsAdmin)
                {
                    dto.DepartmentName = "Toàn hệ thống";
                }
                else if (user.DepartmentId.HasValue)
                {
                    try
                    {
                        var dept = await _dbContext.Departments
                            .AsNoTracking()
                            .FirstOrDefaultAsync(d => d.Id == user.DepartmentId.Value);
                        if (dept != null)
                        {
                            dto.DepartmentName = dept.Name;
                        }
                    }
                    catch { }
                }
                else
                {
                    // Tự động gán phòng ban mặc định cho tài khoản nếu chưa có
                    try
                    {
                        var defaultDept = await _dbContext.Departments
                            .AsNoTracking()
                            .OrderBy(d => d.CreatedAt)
                            .FirstOrDefaultAsync();
                        if (defaultDept != null)
                        {
                            user.DepartmentId = defaultDept.Id;
                            dto.DepartmentId = defaultDept.Id;
                            dto.DepartmentName = defaultDept.Name;
                            await _userManager.UpdateAsync(user);
                        }
                    }
                    catch { }
                }
            }
            else
            {
                dto.DisplayName = principal.Identity?.Name ?? "User";
                dto.IsAdmin = principal.IsInRole("Admin") || (principal.Identity?.Name?.Equals("admin@cms.com", StringComparison.OrdinalIgnoreCase) == true);
                dto.IsQAManager = principal.IsInRole("QA Manager");
                dto.IsQACoordinator = principal.IsInRole("QA Coordinator");
                dto.IsCustomer = principal.IsInRole("Customer");
                if (dto.IsAdmin)
                {
                    dto.DepartmentName = "Toàn hệ thống";
                }
            }

            if (dto.IsAdmin) { dto.RoleName = "Admin"; dto.RoleBadgeClass = "bg-rose-100 text-rose-700"; }
            else if (dto.IsQAManager) { dto.RoleName = "QA Manager"; dto.RoleBadgeClass = "bg-amber-100 text-amber-700"; }
            else if (dto.IsQACoordinator) { dto.RoleName = "QA Coordinator"; dto.RoleBadgeClass = "bg-purple-100 text-purple-700"; }
            else if (dto.IsCustomer) { dto.RoleName = "Customer"; dto.RoleBadgeClass = "bg-emerald-100 text-emerald-700"; }

            // Lưu vào MemoryCache trong 10 phút
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(10));
            httpContext.Items["CURRENT_USER_CACHE"] = dto;

            return dto;
        }

        public void ClearCache(string userId)
        {
            _cache.Remove($"CurrentUserProfile_{userId}");
        }
    }
}

