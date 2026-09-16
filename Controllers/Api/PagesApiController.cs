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
    [Route("api/pages")]
    [ApiController]
    [Tags("Pages")]
    public class PagesApiController : ControllerBase
    {
        private readonly IPageService _pageService;

        public PagesApiController(IPageService pageService)
        {
            _pageService = pageService;
        }

        // GET: api/pages
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var pages = await _pageService.GetAllAsync();
            return Ok(ApiResponse<List<PageDto>>.Ok(pages));
        }

        // GET: api/pages/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var page = await _pageService.GetByIdAsync(id);
            if (page == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy trang."));
            }
            return Ok(ApiResponse<PageDto>.Ok(page));
        }

        // POST: api/pages
        [Authorize(Roles = "Admin,QA Manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PageDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _pageService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể tạo trang."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<PageDto>.Ok(dto, "Tạo trang thành công."));
        }

        // PUT: api/pages/{id}
        [Authorize(Roles = "Admin,QA Manager")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] PageDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(ApiResponse<string>.Fail("Mã trang không khớp."));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _pageService.UpdateAsync(dto);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy trang để cập nhật."));
            }

            return Ok(ApiResponse<PageDto>.Ok(dto, "Cập nhật trang thành công."));
        }

        // DELETE: api/pages/{id}
        [Authorize(Roles = "Admin,QA Manager")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _pageService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy trang để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa trang thành công."));
        }
    }
}

