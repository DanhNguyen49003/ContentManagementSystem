using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ContentManagementSystem.ApplicationCore.DTOs;
using ContentManagementSystem.ApplicationCore.Entities.Identity;
using ContentManagementSystem.Seeders;
using ContentManagementSystem.Services;
using ContentManagementSystem.Services.ApiClients;
using ContentManagementSystem.ViewModels.Auth;

namespace ContentManagementSystem.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ContentUser> _userManager;
        private readonly SignInManager<ContentUser> _signInManager;
        private readonly RoleManager<ContentRole> _roleManager;
        private readonly IApiClient _apiClient;
        private readonly IEmailSender _emailSender;
        private readonly EmailSettings _emailSettings;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ContentUser> userManager,
            SignInManager<ContentUser> signInManager,
            RoleManager<ContentRole> roleManager,
            IApiClient apiClient,
            IEmailSender emailSender,
            IOptions<EmailSettings> emailOptions,
            IWebHostEnvironment webHostEnvironment,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _apiClient = apiClient;
            _emailSender = emailSender;
            _emailSettings = emailOptions.Value;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // ==========================================
        // 1. XÁC THỰC (AUTHENTICATION)
        // ==========================================

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return LocalRedirect(returnUrl ?? "~/");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            returnUrl ??= model.ReturnUrl ?? Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Xác thực thông tin đăng nhập và lấy JWT Token qua Web API
            var loginRequest = new LoginRequestDto
            {
                Email = model.Email,
                Password = model.Password
            };

            var loginResponse = await _apiClient.PostAsync<LoginRequestDto, LoginResponseDto>("api/auth/login", loginRequest);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || loginResponse == null)
            {
                if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn chưa được kích hoạt qua email. Vui lòng kiểm tra hộp thư (hoặc thư mục Spam) để nhấn vào liên kết kích hoạt trước khi đăng nhập.");
                    return View(model);
                }

                if (user != null && await _userManager.IsLockedOutAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                    return View(model);
                }

                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName ?? model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Lưu token JWT từ API vào Identity claim "access_token" để ApiClient tự động gửi Bearer token
                if (!string.IsNullOrEmpty(loginResponse.Token))
                {
                    var userClaims = await _userManager.GetClaimsAsync(user);
                    var oldTokenClaim = userClaims.FirstOrDefault(c => c.Type == "access_token");
                    if (oldTokenClaim != null)
                    {
                        await _userManager.RemoveClaimAsync(user, oldTokenClaim);
                    }
                    await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("access_token", loginResponse.Token));
                    await _signInManager.RefreshSignInAsync(user);
                }

                _logger.LogInformation("Người dùng {Email} đăng nhập thành công qua Web API.", model.Email);
                if (Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Tài khoản {Email} bị khóa do nhập sai nhiều lần.", model.Email);
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị tạm khóa do nhập sai thông tin nhiều lần. Vui lòng thử lại sau.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Địa chỉ Email này đã được đăng ký trong hệ thống.");
                return View(model);
            }

            var user = new ContentUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = false // Bắt buộc kích hoạt qua email Brevo
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Tài khoản mới được tạo: {Email}", user.Email);

                // Mặc định gán vai trò Customer cho tài khoản đăng ký mới
                await _userManager.AddToRoleAsync(user, "Customer");

                // Tạo token xác nhận email và gửi thư qua Brevo
                try
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var callbackUrl = Url.Action(
                        "ConfirmEmail",
                        "Auth",
                        new { userId = user.Id, code = encodedToken },
                        protocol: Request.Scheme);

                    _logger.LogInformation(@"
=======================================================================
🔗 [XÁC NHẬN TÀI KHOẢN] ĐƯỜNG DẪN KÍCH HOẠT CHO: {Email}
{Url}
=======================================================================", user.Email, callbackUrl);

                    var emailHtml = EmailTemplateHelper.GenerateConfirmationEmail(callbackUrl ?? "", user.FullName ?? "");
                    await _emailSender.SendEmailAsync(user.Email!, "Xác nhận kích hoạt tài khoản - CMS Portal", emailHtml);

                    TempData["DevConfirmLink"] = callbackUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi email xác nhận cho {Email}", user.Email);
                }

                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Chúng tôi đã gửi một email xác thực đến địa chỉ của bạn. Vui lòng kiểm tra hộp thư (hoặc thư mục Spam) và nhấn vào liên kết để kích hoạt tài khoản trước khi đăng nhập.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Auth/ConfirmEmail
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string? userId, string? code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                TempData["ErrorMessage"] = "Liên kết kích hoạt tài khoản không hợp lệ.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin tài khoản người dùng.";
                return RedirectToAction(nameof(Login));
            }

            try
            {
                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Tài khoản của bạn đã được xác thực thành công! Hãy đăng nhập để tiếp tục.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Xác nhận email thất bại hoặc liên kết đã hết hạn.";
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Liên kết kích hoạt không hợp lệ.";
            }

            return RedirectToAction(nameof(Login));
        }

        // POST: /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Người dùng đã đăng xuất khỏi hệ thống.");
            return RedirectToAction(nameof(Login));
        }

        // GET: /Auth/Logout (hỗ trợ cả GET khi người dùng click link đăng xuất)
        [HttpGet]
        public async Task<IActionResult> LogoutGet()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        // GET: /Auth/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Auth/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                try
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var callbackUrl = Url.Action(
                        "ResetPassword",
                        "Auth",
                        new { code = encodedToken, email = model.Email },
                        protocol: Request.Scheme);

                    _logger.LogInformation(@"
=======================================================================
🔑 [KHÔI PHỤC MẬT KHẨU] ĐƯỜNG DẪN ĐẶT LẠI MẬT KHẨU CHO: {Email}
{Url}
=======================================================================", model.Email, callbackUrl);

                    var emailHtml = EmailTemplateHelper.GenerateResetPasswordEmail(callbackUrl ?? "", user.FullName ?? user.Email ?? "");
                    await _emailSender.SendEmailAsync(model.Email, "Yêu cầu đặt lại mật khẩu - CMS Portal", emailHtml);

                    TempData["DevResetLink"] = callbackUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi email đặt lại mật khẩu cho {Email}", model.Email);
                }
            }

            TempData["SuccessMessage"] = "Nếu địa chỉ Email của bạn tồn tại trên hệ thống, thư hướng dẫn khôi phục mật khẩu đã được gửi đi.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /Auth/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string? code = null, string? email = null)
        {
            if (string.IsNullOrEmpty(code))
            {
                TempData["ErrorMessage"] = "Yêu cầu khôi phục mật khẩu không hợp lệ.";
                return RedirectToAction(nameof(Login));
            }

            return View(new ResetPasswordViewModel
            {
                Code = code,
                Email = email ?? string.Empty
            });
        }

        // POST: /Auth/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["SuccessMessage"] = "Mật khẩu đã được cập nhật thành công.";
                return RedirectToAction(nameof(Login));
            }

            try
            {
                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
                var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập với mật khẩu mới.";
                    return RedirectToAction(nameof(Login));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Mã xác thực không hợp lệ hoặc đã hết hạn.");
            }

            return View(model);
        }

        // GET: /Auth/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==========================================
        // 2. TÀI KHOẢN CÁ NHÂN (PROFILE & SECURITY)
        // ==========================================

        // GET: /Auth/Profile
        [Authorize]
        [HttpGet]
        public IActionResult Profile()
        {
            return RedirectToAction("Index", "Profile");
        }

        // POST: /Auth/Profile
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(UserProfileViewModel model)
        {
            return RedirectToAction("Index", "Profile");
        }

        // GET: /Auth/ChangePassword
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Auth/ChangePassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Không tìm thấy thông tin tài khoản.");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ==========================================
        // 3. QUẢN TRỊ IDENTITY (ADMIN ONLY)
        // ==========================================

        // GET: /Auth/Users
        [Authorize(Roles = "Admin,QA Manager")]
        [HttpGet]
        public async Task<IActionResult> Users(string? search = null, string? role = null)
        {
            var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var userList = new List<UserManagementViewModel>();

            foreach (var user in users)
            {
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var matchEmail = user.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false;
                    var matchName = user.FullName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false;
                    if (!matchEmail && !matchName)
                    {
                        continue;
                    }
                }

                var roles = await _userManager.GetRolesAsync(user);

                if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                userList.Add(new UserManagementViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    Avatar = user.Avatar,
                    Roles = roles,
                    LockoutEnd = user.LockoutEnd,
                    CreatedAt = user.CreatedAt,
                    EmailConfirmed = user.EmailConfirmed
                });
            }

            ViewBag.Search = search;
            ViewBag.SelectedRole = role;
            ViewBag.AllRoles = IdentityDataSeeder.Roles;

            return View(userList);
        }

        // GET: /Auth/AssignRole/5
        [Authorize(Roles = "Admin,QA Manager")]
        [HttpGet]
        public async Task<IActionResult> AssignRole(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("Mã người dùng không hợp lệ.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            var model = new AssignRoleViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                CurrentRoles = currentRoles,
                SelectedRole = currentRoles.FirstOrDefault() ?? "Customer",
                AvailableRoles = IdentityDataSeeder.Roles.ToList()
            };

            return View(model);
        }

        // POST: /Auth/AssignRole
        [Authorize(Roles = "Admin,QA Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(AssignRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = IdentityDataSeeder.Roles.ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Gỡ bỏ các vai trò cũ và gán vai trò mới
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi thu hồi vai trò cũ.");
                model.AvailableRoles = IdentityDataSeeder.Roles.ToList();
                return View(model);
            }

            var addResult = await _userManager.AddToRoleAsync(user, model.SelectedRole);
            if (!addResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi gán vai trò mới.");
                model.AvailableRoles = IdentityDataSeeder.Roles.ToList();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Đã cập nhật vai trò [{model.SelectedRole}] cho người dùng {user.Email} thành công!";
            return RedirectToAction(nameof(Users));
        }

        // POST: /Auth/ToggleLock/5
        [Authorize(Roles = "Admin,QA Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản quản trị của chính mình!";
                return RedirectToAction(nameof(Users));
            }

            var isLocked = await _userManager.IsLockedOutAsync(user);
            if (isLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = $"Đã mở khóa tài khoản {user.Email} thành công!";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                TempData["SuccessMessage"] = $"Đã khóa tài khoản {user.Email} thành công!";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: /Auth/ConfirmUserEmail/5
        [Authorize(Roles = "Admin,QA Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmUserEmail(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Đã kích hoạt email xác nhận cho tài khoản {user.Email} thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật trạng thái email cho tài khoản này.";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: /Auth/DeleteUser/5
        [Authorize(Roles = "Admin,QA Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản của chính mình!";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Đã xóa người dùng {user.Email} khỏi hệ thống!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa người dùng này.";
            }

            return RedirectToAction(nameof(Users));
        }

        // GET: /Auth/Roles
        [Authorize(Roles = "Admin,QA Manager")]
        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var roleViewModels = new List<RoleManagementViewModel>();

            var roleDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Admin", "Quản trị viên: Toàn quyền quản trị hệ thống, người dùng, phân quyền vai trò, cài đặt giao diện, bài viết và bảo mật." },
                { "QA Manager", "Trưởng ban QA: Phê duyệt bài viết, kiểm soát chất lượng nội dung, quản lý tiêu chuẩn xuất bản và báo cáo." },
                { "QA Coordinator", "Điều phối viên QA: Tiếp nhận, đánh giá bài viết mới từ cộng đồng/khách hàng và điều phối quy trình duyệt." },
                { "Customer", "Khách hàng / Thành viên: Tạo và đóng góp bài viết, gửi bài chờ kiểm duyệt và quản lý hồ sơ cá nhân." }
            };

            foreach (var role in roles)
            {
                var roleName = role.Name ?? string.Empty;
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

                roleViewModels.Add(new RoleManagementViewModel
                {
                    Id = role.Id,
                    Name = roleName,
                    NormalizedName = role.NormalizedName,
                    UserCount = usersInRole.Count,
                    Description = roleDescriptions.TryGetValue(roleName, out var desc) ? desc : "Vai trò hệ thống."
                });
            }

            return View(roleViewModels);
        }

        // GET: /Auth/TestSmtp
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestSmtp(string? toEmail)
        {
            var targetEmail = toEmail ?? "danh49003@gmail.com";
            var sb = new StringBuilder();
            sb.AppendLine("=== CHẨN ĐOÁN KẾT NỐI BREVO SMTP ===");
            sb.AppendLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Server: {_emailSettings.SmtpServer}:{_emailSettings.SmtpPort}");
            sb.AppendLine($"Sender Email: {_emailSettings.SenderEmail}");
            sb.AppendLine($"Configured Username: {_emailSettings.Username}");
            sb.AppendLine($"Password length: {_emailSettings.Password?.Length ?? 0}");
            sb.AppendLine();

            string[] testUsers = new[] { _emailSettings.Username, _emailSettings.SenderEmail };
            foreach (var testUser in testUsers.Distinct())
            {
                sb.AppendLine($"--- THỬ ĐĂNG NHẬP VỚI USERNAME: '{testUser}' ---");
                using var client = new MailKit.Net.Smtp.SmtpClient();
                client.Timeout = 10000;
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                try
                {
                    sb.AppendLine($"1. Kết nối tới {_emailSettings.SmtpServer}:{_emailSettings.SmtpPort} (StartTls)...");
                    await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    sb.AppendLine($"   -> Kết nối thành công! Các cơ chế Auth hỗ trợ: {string.Join(", ", client.AuthenticationMechanisms)}");

                    sb.AppendLine($"2. Đang xác thực với user '{testUser}'...");
                    await client.AuthenticateAsync(testUser, _emailSettings.Password);
                    sb.AppendLine($"   -> XÁC THỰC THÀNH CÔNG RỰC RỠ VỚI USER '{testUser}'! 🎉");

                    sb.AppendLine($"3. Gửi email thử nghiệm từ {_emailSettings.SenderEmail} tới {targetEmail}...");
                    var msg = new MimeKit.MimeMessage();
                    msg.From.Add(new MimeKit.MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                    msg.To.Add(new MimeKit.MailboxAddress("User Test", targetEmail));
                    msg.Subject = $"Thử nghiệm Brevo SMTP thành công [{DateTime.Now:HH:mm:ss}]";
                    msg.Body = new MimeKit.TextPart("html")
                    {
                        Text = $"<h3>Xin chào!</h3><p>Email này được gửi thành công từ Brevo SMTP với Username: <b>{testUser}</b></p>"
                    };

                    await client.SendAsync(msg);
                    sb.AppendLine($"   -> GỬI THƯ THÀNH CÔNG 100% TỚI {targetEmail}! 🚀");
                    await client.DisconnectAsync(true);
                    break;
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"   -> ❌ THẤT BẠI: [{ex.GetType().Name}] {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        sb.AppendLine($"      Chi tiết: {ex.InnerException.Message}");
                    }
                    try { await client.DisconnectAsync(true); } catch { }
                }
                sb.AppendLine();
            }

            return Content(sb.ToString(), "text/plain; charset=utf-8");
        }
    }
}

