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
    [Route("api/tags")]
    [ApiController]
    [Tags("Tags")]
    public class TagsApiController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagsApiController(ITagService tagService)
        {
            _tagService = tagService;
        }

        // GET: api/tags
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tags = await _tagService.GetAllAsync();
            return Ok(ApiResponse<List<TagDto>>.Ok(tags));
        }

        // GET: api/tags/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tag = await _tagService.GetByIdAsync(id);
            if (tag == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy thẻ tag."));
            }
            return Ok(ApiResponse<TagDto>.Ok(tag));
        }

        // POST: api/tags
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TagDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _tagService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể tạo thẻ tag mới."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<TagDto>.Ok(dto, "Tạo thẻ tag thành công."));
        }

        // PUT: api/tags/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] TagDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(ApiResponse<string>.Fail("Mã thẻ tag không khớp."));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _tagService.UpdateAsync(dto);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy thẻ tag để cập nhật."));
            }

            return Ok(ApiResponse<TagDto>.Ok(dto, "Cập nhật thẻ tag thành công."));
        }

        // DELETE: api/tags/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _tagService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy thẻ tag để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa thẻ tag thành công."));
        }
    }
}

