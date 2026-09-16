using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ContentManagementSystem.ApplicationCore.DTOs;
using ContentManagementSystem.Service.Interface;

namespace ContentManagementSystem.Controllers.Api
{
    [Route("api/banners")]
    [ApiController]
    [Tags("Banners")]
    public class BannersApiController : ControllerBase
    {
        private readonly IBannerService _bannerService;

        public BannersApiController(IBannerService bannerService)
        {
            _bannerService = bannerService;
        }

        // GET: api/banners
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var banners = await _bannerService.GetAllAsync();
            return Ok(ApiResponse<List<BannerDto>>.Ok(banners));
        }

        // GET: api/banners/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var banner = await _bannerService.GetByIdAsync(id);
            if (banner == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy banner."));
            }
            return Ok(ApiResponse<BannerDto>.Ok(banner));
        }

        // POST: api/banners
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BannerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _bannerService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể tạo banner."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<BannerDto>.Ok(dto, "Tạo banner thành công."));
        }

        // PUT: api/banners/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] BannerDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(ApiResponse<string>.Fail("Mã banner không khớp."));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _bannerService.UpdateAsync(dto);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy banner để cập nhật."));
            }

            return Ok(ApiResponse<BannerDto>.Ok(dto, "Cập nhật banner thành công."));
        }

        // DELETE: api/banners/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _bannerService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy banner để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa banner thành công."));
        }
    }
}

