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
    [Route("api/categories")]
    [ApiController]
    [Tags("Categories")]
    public class CategoriesApiController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesApiController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(ApiResponse<List<CategoryDto>>.Ok(categories));
        }

        // GET: api/categories/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy chuyên mục."));
            }
            return Ok(ApiResponse<CategoryDto>.Ok(category));
        }

        // POST: api/categories
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _categoryService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể tạo chuyên mục."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<CategoryDto>.Ok(dto, "Tạo chuyên mục thành công."));
        }

        // PUT: api/categories/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CategoryDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(ApiResponse<string>.Fail("Mã chuyên mục không khớp."));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _categoryService.UpdateAsync(dto);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy chuyên mục để cập nhật."));
            }

            return Ok(ApiResponse<CategoryDto>.Ok(dto, "Cập nhật chuyên mục thành công."));
        }

        // DELETE: api/categories/{id}
        [Authorize(Roles = "Admin,QA Manager")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _categoryService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy chuyên mục để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa chuyên mục thành công."));
        }
    }
}

