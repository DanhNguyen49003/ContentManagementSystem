using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ContentManagementSystem.ApplicationCore.Entities.Identity;
using ContentManagementSystem.ViewModels.Auth;

namespace ContentManagementSystem.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class ProfileController : Controller
    {
        private readonly UserManager<ContentUser> _userManager;
        private readonly SignInManager<ContentUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            UserManager<ContentUser> userManager,
            SignInManager<ContentUser> signInManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: /Profile or /Profile/Index
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? tab = "info")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Không tìm thấy thông tin tài khoản.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserProfileViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Avatar = user.Avatar,
                Roles = roles,
                CreatedAt = user.CreatedAt
            };

            ViewBag.ActiveTab = tab ?? "info";
            ViewBag.ChangePasswordModel = new ChangePasswordViewModel();
            return View(model);
        }

        // POST: /Profile/Update
        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(UserProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Không tìm thấy thông tin tài khoản.");
            }

            if (!ModelState.IsValid)
            {
                model.Email = user.Email ?? string.Empty;
                model.Roles = await _userManager.GetRolesAsync(user);
                model.CreatedAt = user.CreatedAt;
                ViewBag.ActiveTab = "info";
                ViewBag.ChangePasswordModel = new ChangePasswordViewModel();
                return View("Index", model);
            }

            // Xử lý upload avatar từ máy tính
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var ext = Path.GetExtension(model.AvatarFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("AvatarFile", "Chỉ chấp nhận file ảnh có định dạng .jpg, .jpeg, .png, .webp, .gif");
                    model.Email = user.Email ?? string.Empty;
                    model.Roles = await _userManager.GetRolesAsync(user);
                    model.CreatedAt = user.CreatedAt;
                    ViewBag.ActiveTab = "info";
                    ViewBag.ChangePasswordModel = new ChangePasswordViewModel();
                    return View("Index", model);
                }

                if (model.AvatarFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("AvatarFile", "Kích thước ảnh đại diện không được vượt quá 5MB.");
                    model.Email = user.Email ?? string.Empty;
                    model.Roles = await _userManager.GetRolesAsync(user);
                    model.CreatedAt = user.CreatedAt;
                    ViewBag.ActiveTab = "info";
                    ViewBag.ChangePasswordModel = new ChangePasswordViewModel();
                    return View("Index", model);
                }

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"avatar_{user.Id}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(stream);
                }

                user.Avatar = $"/uploads/avatars/{fileName}";
            }
            else if (!string.IsNullOrWhiteSpace(model.Avatar))
            {
                user.Avatar = model.Avatar.Trim();
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Cập nhật thông tin hồ sơ và ảnh đại diện thành công!";
                return RedirectToAction(nameof(Index), new { tab = "info" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Email = user.Email ?? string.Empty;
            model.Roles = await _userManager.GetRolesAsync(user);
            model.CreatedAt = user.CreatedAt;
            ViewBag.ActiveTab = "info";
            ViewBag.ChangePasswordModel = new ChangePasswordViewModel();

            return View("Index", model);
        }

        // POST: /Profile/ChangePassword
        [HttpPost("ChangePassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel passwordModel)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Không tìm thấy thông tin tài khoản.");
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["PasswordErrorMessage"] = $"Dữ liệu đổi mật khẩu không hợp lệ: {errors}";
                return RedirectToAction(nameof(Index), new { tab = "security" });
            }

            var result = await _userManager.ChangePasswordAsync(user, passwordModel.CurrentPassword, passwordModel.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Đổi mật khẩu tài khoản thành công!";
                return RedirectToAction(nameof(Index), new { tab = "security" });
            }

            var errDesc = string.Join("; ", result.Errors.Select(e => e.Description));
            TempData["PasswordErrorMessage"] = $"Đổi mật khẩu thất bại: {errDesc}";
            return RedirectToAction(nameof(Index), new { tab = "security" });
        }
    }
}

