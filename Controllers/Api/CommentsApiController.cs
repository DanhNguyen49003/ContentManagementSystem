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
    [Route("api/comments")]
    [ApiController]
    [Tags("Comments")]
    public class CommentsApiController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsApiController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        // GET: api/comments
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var comments = await _commentService.GetAllAsync();
            return Ok(ApiResponse<List<CommentDto>>.Ok(comments));
        }

        // GET: api/comments/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var comment = await _commentService.GetByIdAsync(id);
            if (comment == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy bình luận."));
            }
            return Ok(ApiResponse<CommentDto>.Ok(comment));
        }

        // POST: api/comments
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CommentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _commentService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể gửi bình luận."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<CommentDto>.Ok(dto, "Gửi bình luận thành công."));
        }

        // DELETE: api/comments/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _commentService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy bình luận để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa bình luận thành công."));
        }

        // PATCH: api/comments/{id}/approve
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPatch("{id:guid}/approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            var success = await _commentService.ApproveAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy bình luận."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Đã duyệt bình luận."));
        }

        // PATCH: api/comments/{id}/reject
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPatch("{id:guid}/reject")]
        public async Task<IActionResult> Reject(Guid id)
        {
            var success = await _commentService.RejectAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy bình luận."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Đã từ chối bình luận."));
        }
    }
}

