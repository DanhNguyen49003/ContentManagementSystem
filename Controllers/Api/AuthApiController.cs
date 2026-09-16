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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ContentManagementSystem.ApplicationCore.DTOs;
using ContentManagementSystem.ApplicationCore.Entities.Identity;

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

        public AuthApiController(
            UserManager<ContentUser> userManager,
            SignInManager<ContentUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
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

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return Unauthorized(ApiResponse<string>.Fail("Tài khoản của bạn chưa được kích hoạt qua email. Vui lòng kiểm tra hộp thư để kích hoạt tài khoản trước khi đăng nhập."));
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return Unauthorized(ApiResponse<string>.Fail("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên."));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return Unauthorized(ApiResponse<string>.Fail("Email hoặc mật khẩu không chính xác."));
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Sinh JWT Token
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

            var response = new LoginResponseDto
            {
                Token = tokenString,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Roles = roles,
                ExpiresAt = expires
            };

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

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<string>.Fail("Địa chỉ Email này đã được đăng ký trong hệ thống."));
            }

            var user = new ContentUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");

                return Ok(ApiResponse<string>.Ok(user.Id, "Đăng ký tài khoản thành công! Vui lòng kiểm tra email để kích hoạt tài khoản."));
            }

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(ApiResponse<string>.Fail($"Đăng ký thất bại: {errors}"));
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

