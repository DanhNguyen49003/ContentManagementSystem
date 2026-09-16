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
    [Route("api/faqs")]
    [ApiController]
    [Tags("Faqs")]
    public class FaqsApiController : ControllerBase
    {
        private readonly IFaqService _faqService;

        public FaqsApiController(IFaqService faqService)
        {
            _faqService = faqService;
        }

        // GET: api/faqs
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var faqs = await _faqService.GetAllAsync();
            return Ok(ApiResponse<List<FaqDto>>.Ok(faqs));
        }

        // GET: api/faqs/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var faq = await _faqService.GetByIdAsync(id);
            if (faq == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy câu hỏi FAQ."));
            }
            return Ok(ApiResponse<FaqDto>.Ok(faq));
        }

        // POST: api/faqs
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FaqDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _faqService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể tạo câu hỏi FAQ."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<FaqDto>.Ok(dto, "Tạo FAQ thành công."));
        }

        // PUT: api/faqs/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] FaqDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(ApiResponse<string>.Fail("Mã FAQ không khớp."));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _faqService.UpdateAsync(dto);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy FAQ để cập nhật."));
            }

            return Ok(ApiResponse<FaqDto>.Ok(dto, "Cập nhật FAQ thành công."));
        }

        // DELETE: api/faqs/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _faqService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy FAQ để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa FAQ thành công."));
        }
    }
}

