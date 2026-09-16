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
    [Route("api/posts")]
    [ApiController]
    [Tags("Posts")]
    public class PostsApiController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostsApiController(IPostService postService)
        {
            _postService = postService;
        }

        // GET: api/posts
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var posts = await _postService.GetAllAsync();
            return Ok(ApiResponse<List<PostDto>>.Ok(posts));
        }

        // GET: api/posts/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var post = await _postService.GetByIdAsync(id);
            if (post == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy bài viết."));
            }
            return Ok(ApiResponse<PostDto>.Ok(post));
        }

        // POST: api/posts
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator,Customer")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PostDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _postService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể tạo bài viết mới."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<PostDto>.Ok(dto, "Tạo bài viết thành công."));
        }

        // PUT: api/posts/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator,Customer")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] PostDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(ApiResponse<string>.Fail("Mã bài viết không khớp."));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _postService.UpdateAsync(dto);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy bài viết để cập nhật."));
            }

            return Ok(ApiResponse<PostDto>.Ok(dto, "Cập nhật bài viết thành công."));
        }

        // DELETE: api/posts/{id}
        [Authorize(Roles = "Admin,QA Manager")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _postService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy bài viết để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa bài viết thành công."));
        }

        // PATCH: api/posts/{id}/toggle-publish
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPatch("{id:guid}/toggle-publish")]
        public async Task<IActionResult> TogglePublish(Guid id)
        {
            var success = await _postService.TogglePublishAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy bài viết."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Thay đổi trạng thái xuất bản thành công."));
        }
    }
}

