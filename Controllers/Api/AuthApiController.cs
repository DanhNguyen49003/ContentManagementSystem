using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ContentManagementSystem.ApplicationCore.DTOs;
using ContentManagementSystem.ApplicationCore.Entities.Identity;
using ContentManagementSystem.Services;

namespace ContentManagementSystem.Controllers.Api
{
    [Route("api/auth")]
    [ApiController]
    [Tags("Auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<ContentUser> _userManager;
        private readonly SignInManager<ContentUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(
            UserManager<ContentUser> userManager,
            SignInManager<ContentUser> signInManager,
            IConfiguration configuration,
            IMemoryCache memoryCache,
            IEmailSender emailSender,
            ILogger<AuthApiController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _emailSender = emailSender;
            _logger = logger;
        }

        private LoginResponseDto GenerateJwtToken(ContentUser user, IList<string> roles)
        {
            var jwtSection = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSection["SecretKey"] ?? "DefaultFallbackSecretKeyForCMSPortalJwtToken2026!";
            var issuer = jwtSection["Issuer"] ?? "CMSPortalAPI";
            var audience = jwtSection["Audience"] ?? "CMSPortalClients";
            var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var minutes) ? minutes : 1440;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new LoginResponseDto
            {
                Token = tokenString,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Roles = roles,
                ExpiresAt = expires
            };
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu gửi lên không hợp lệ."));
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(ApiResponse<string>.Fail("Email hoặc mật khẩu không chính xác."));
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
            {
                return Unauthorized(ApiResponse<string>.Fail("Email hoặc mật khẩu không chính xác."));
            }

            // Kiểm tra trạng thái xác thực Email qua OTP
            if (!user.EmailConfirmed)
            {
                return Unauthorized(ApiResponse<string>.Fail("Tài khoản chưa được kích hoạt qua mã OTP. Vui lòng xác thực mã OTP trước khi đăng nhập."));
            }

            if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                return Unauthorized(ApiResponse<string>.Fail("Tài khoản của bạn đang bị tạm khóa. Vui lòng liên hệ quản trị viên hoặc thử lại sau."));
            }

            if (user.AccessFailedCount > 0)
            {
                user.AccessFailedCount = 0;
                await _userManager.UpdateAsync(user);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var response = GenerateJwtToken(user, roles);

            return Ok(ApiResponse<LoginResponseDto>.Ok(response, "Đăng nhập thành công!"));
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu gửi lên không hợp lệ."));
            }

            var email = model.Email.Trim().ToLowerInvariant();
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<string>.Fail("Địa chỉ Email này đã được đăng ký trong hệ thống."));
            }

            var user = new ContentUser
            {
                UserName = email,
                Email = email,
                FullName = model.FullName,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = false // Bắt buộc xác thực qua mã OTP
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");

                // Bước 2: Sinh mã OTP ngẫu nhiên 6 chữ số và lưu vào cache 5 phút
                var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                var cacheKey = $"Register_OTP_{email}";
                var cooldownKey = $"Register_OTP_Cooldown_{email}";
                var attemptsKey = $"Register_OTP_Attempts_{email}";

                _memoryCache.Set(cacheKey, otp, TimeSpan.FromMinutes(5));
                _memoryCache.Set(cooldownKey, DateTime.UtcNow.AddSeconds(60), TimeSpan.FromSeconds(60));
                _memoryCache.Set(attemptsKey, 0, TimeSpan.FromMinutes(5));

                // Bước 3: Phân phối mã qua Email
                try
                {
                    var emailHtml = EmailTemplateHelper.GenerateOtpVerificationEmail(otp, user.FullName ?? user.Email, 5);
                    await _emailSender.SendEmailAsync(user.Email!, "Mã xác thực OTP đăng ký tài khoản - CMS Portal", emailHtml);
                    _logger.LogInformation("Đã gửi mã OTP đăng ký tới {Email} (Mã: {Otp})", user.Email, otp);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi email OTP cho {Email}", user.Email);
                }

                // Tiêu chuẩn bảo mật: Không bao giờ trả mã OTP trong response JSON
                return Ok(ApiResponse<string>.Ok(user.Id, "Đăng ký tài khoản thành công! Mã OTP xác thực gồm 6 chữ số đã được gửi tới email của bạn. Vui lòng gửi mã đến endpoint /api/auth/verify-otp để kích hoạt tài khoản."));
            }

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(ApiResponse<string>.Fail($"Đăng ký thất bại: {errors}"));
        }

        // POST: api/auth/send-otp (Bước 1, 2, 3 trong quy trình chuẩn)
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu gửi lên không hợp lệ."));
            }

            var email = model.Email.Trim().ToLowerInvariant();
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return BadRequest(ApiResponse<string>.Fail("Không tìm thấy thông tin tài khoản ứng với email này."));
            }

            if (user.EmailConfirmed)
            {
                return BadRequest(ApiResponse<string>.Fail("Tài khoản này đã được xác thực trước đó. Bạn có thể đăng nhập ngay mà không cần mã OTP."));
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return BadRequest(ApiResponse<string>.Fail("Tài khoản đang bị tạm khóa. Vui lòng liên hệ quản trị viên."));
            }

            // Tiêu chuẩn bảo mật 1: Rate Limiting (Cooldown 60 giây giữa các lần gửi mã)
            var cooldownKey = $"Register_OTP_Cooldown_{email}";
            if (_memoryCache.TryGetValue(cooldownKey, out DateTime cooldownEnd))
            {
                var remainingSeconds = Math.Max(1, (int)(cooldownEnd - DateTime.UtcNow).TotalSeconds);
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    ApiResponse<string>.Fail($"Yêu cầu gửi mã quá nhanh. Vui lòng đợi {remainingSeconds} giây trước khi yêu cầu mã mới."));
            }

            // Bước 2: Tạo và lưu trữ mã (Generate & Store)
            var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var cacheKey = $"Register_OTP_{email}";
            var attemptsKey = $"Register_OTP_Attempts_{email}";

            _memoryCache.Set(cacheKey, otp, TimeSpan.FromMinutes(5)); // TTL 5 phút
            _memoryCache.Set(cooldownKey, DateTime.UtcNow.AddSeconds(60), TimeSpan.FromSeconds(60)); // Cooldown 60s
            _memoryCache.Set(attemptsKey, 0, TimeSpan.FromMinutes(5)); // Reset bộ đếm thử sai

            // Bước 3: Phân phối mã (Dispatch qua SMTP)
            try
            {
                var emailHtml = EmailTemplateHelper.GenerateOtpVerificationEmail(otp, user.FullName ?? user.Email, 5);
                await _emailSender.SendEmailAsync(user.Email!, "Mã xác thực OTP tài khoản - CMS Portal", emailHtml);
                _logger.LogInformation("Gửi mã OTP thành công cho {Email} (Mã: {Otp})", user.Email, otp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email OTP cho {Email}", user.Email);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse<string>.Fail("Lỗi hệ thống khi gửi email OTP. Vui lòng kiểm tra lại dịch vụ SMTP."));
            }

            // Tiêu chuẩn bảo mật 3: Tuyệt đối không trả về mã OTP trong JSON
            return Ok(ApiResponse<string>.Ok(user.Email!, "Mã OTP 6 chữ số đã được gửi tới email của bạn. Mã có hiệu lực trong 5 phút."));
        }

        // POST: api/auth/verify-otp (Bước 4 & 5 trong quy trình chuẩn)
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu gửi lên không hợp lệ."));
            }

            var email = model.Email.Trim().ToLowerInvariant();
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return BadRequest(ApiResponse<string>.Fail("Không tìm thấy thông tin tài khoản với email này."));
            }

            if (user.EmailConfirmed)
            {
                return BadRequest(ApiResponse<string>.Fail("Tài khoản đã được xác thực trước đó. Mã OTP không còn hiệu lực."));
            }

            var cacheKey = $"Register_OTP_{email}";
            var attemptsKey = $"Register_OTP_Attempts_{email}";
            var lockoutKey = $"Register_OTP_Lockout_{email}";

            // Tiêu chuẩn bảo mật 2: Kiểm tra khóa tạm thời do vượt quá Max Attempts
            if (_memoryCache.TryGetValue(lockoutKey, out DateTime lockoutEnd))
            {
                var remainingMinutes = Math.Max(1, (int)(lockoutEnd - DateTime.UtcNow).TotalMinutes);
                return BadRequest(ApiResponse<string>.Fail($"Chức năng xác thực OTP bị tạm khóa do nhập sai quá 5 lần. Vui lòng thử lại sau {remainingMinutes} phút."));
            }

            // Kiểm tra mã OTP trong cache và TTL (Time-To-Live)
            if (!_memoryCache.TryGetValue(cacheKey, out string? cachedOtp) || string.IsNullOrEmpty(cachedOtp))
            {
                return BadRequest(ApiResponse<string>.Fail("Mã OTP đã hết hiệu lực (quá 5 phút) hoặc chưa được tạo. Vui lòng gửi yêu cầu cấp mã mới."));
            }

            // Tiêu chuẩn bảo mật 2: Max Attempts (Giới hạn tối đa 5 lần thử sai)
            _memoryCache.TryGetValue(attemptsKey, out int currentAttempts);

            if (!string.Equals(cachedOtp, model.OtpCode.Trim(), StringComparison.Ordinal))
            {
                currentAttempts++;
                _memoryCache.Set(attemptsKey, currentAttempts, TimeSpan.FromMinutes(5));

                const int maxAllowedAttempts = 5;
                if (currentAttempts >= maxAllowedAttempts)
                {
                    // Vô hiệu hóa mã ngay lập tức và khóa tạm 15 phút chống brute-force
                    _memoryCache.Remove(cacheKey);
                    _memoryCache.Remove(attemptsKey);
                    _memoryCache.Set(lockoutKey, DateTime.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(15));

                    return BadRequest(ApiResponse<string>.Fail("Bạn đã nhập sai mã OTP 5 lần liên tiếp. Mã xác thực hiện tại đã bị hủy và tính năng xác thực tạm khóa 15 phút."));
                }

                int remaining = maxAllowedAttempts - currentAttempts;
                return BadRequest(ApiResponse<string>.Fail($"Mã OTP không chính xác. Bạn còn {remaining} lần thử trước khi mã bị hủy."));
            }

            // Bước 5: Validation thành công & Hoàn tất kích hoạt
            user.EmailConfirmed = true;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            await _userManager.UpdateAsync(user);

            // BẮT BUỘC: Xóa ngay mã OTP khỏi cache để chống tấn công phát lại (Replay Attack)
            _memoryCache.Remove(cacheKey);
            _memoryCache.Remove(attemptsKey);
            _memoryCache.Remove($"Register_OTP_Cooldown_{email}");

            _logger.LogInformation("Tài khoản {Email} đã xác thực mã OTP thành công qua API.", user.Email);

            // Cấp phát ngay JWT Token cho Client
            var roles = await _userManager.GetRolesAsync(user);
            var tokenResponse = GenerateJwtToken(user, roles);

            return Ok(ApiResponse<LoginResponseDto>.Ok(tokenResponse, "Xác thực mã OTP thành công! Tài khoản của bạn đã được kích hoạt."));
        }

        // GET: api/auth/me
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<string>.Fail("Chưa đăng nhập hoặc phiên đã hết hạn."));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy thông tin tài khoản."));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userInfo = new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Avatar = user.Avatar,
                Roles = roles,
                CreatedAt = user.CreatedAt
            };

            return Ok(ApiResponse<UserInfoDto>.Ok(userInfo, "Lấy thông tin tài khoản thành công."));
        }

        // GET: api/auth/users
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var userList = new List<UserInfoDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    Avatar = user.Avatar,
                    Roles = roles,
                    CreatedAt = user.CreatedAt
                });
            }

            return Ok(ApiResponse<List<UserInfoDto>>.Ok(userList, "Lấy danh sách người dùng thành công."));
        }

        // GET: api/auth/authors
        [Authorize]
        [HttpGet("authors")]
        public async Task<IActionResult> GetAuthors()
        {
            var users = await _userManager.Users.OrderBy(u => u.FullName ?? u.Email).ToListAsync();
            var authors = users.Select(u => new UserInfoDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FullName = string.IsNullOrWhiteSpace(u.FullName) ? (u.Email ?? "Tác giả") : u.FullName,
                Avatar = u.Avatar
            }).ToList();

            return Ok(ApiResponse<List<UserInfoDto>>.Ok(authors, "Lấy danh sách tác giả thành công."));
        }
    }
}

