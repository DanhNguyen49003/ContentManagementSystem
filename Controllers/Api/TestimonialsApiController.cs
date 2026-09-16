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
    [Route("api/testimonials")]
    [ApiController]
    [Tags("Testimonials")]
    public class TestimonialsApiController : ControllerBase
    {
        private readonly ITestimonialService _testimonialService;

        public TestimonialsApiController(ITestimonialService testimonialService)
        {
            _testimonialService = testimonialService;
        }

        // GET: api/testimonials
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var testimonials = await _testimonialService.GetAllAsync();
            return Ok(ApiResponse<List<TestimonialDto>>.Ok(testimonials));
        }

        // GET: api/testimonials/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var testimonial = await _testimonialService.GetByIdAsync(id);
            if (testimonial == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy nhận xét."));
            }
            return Ok(ApiResponse<TestimonialDto>.Ok(testimonial));
        }

        // POST: api/testimonials
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TestimonialDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _testimonialService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể tạo nhận xét."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<TestimonialDto>.Ok(dto, "Tạo nhận xét thành công."));
        }

        // PUT: api/testimonials/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] TestimonialDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(ApiResponse<string>.Fail("Mã nhận xét không khớp."));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _testimonialService.UpdateAsync(dto);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy nhận xét để cập nhật."));
            }

            return Ok(ApiResponse<TestimonialDto>.Ok(dto, "Cập nhật nhận xét thành công."));
        }

        // DELETE: api/testimonials/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _testimonialService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy nhận xét để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa nhận xét thành công."));
        }
    }
}

