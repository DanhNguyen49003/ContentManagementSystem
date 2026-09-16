using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ContentManagementSystem.ApplicationCore.DTOs;
using ContentManagementSystem.Service.Interface;

namespace ContentManagementSystem.Controllers.Api
{
    [Route("api/newslettersubscribers")]
    [Route("api/newsletter-subscribers")]
    [ApiController]
    [Tags("NewsletterSubscribers")]
    public class NewsletterSubscribersApiController : ControllerBase
    {
        private readonly INewsletterSubscriberService _subscriberService;

        public NewsletterSubscribersApiController(INewsletterSubscriberService subscriberService)
        {
            _subscriberService = subscriberService;
        }

        public class SubscribeRequest
        {
            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            public string Email { get; set; } = string.Empty;
        }

        // GET: api/newslettersubscribers
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var subscribers = await _subscriberService.GetAllAsync();
            return Ok(ApiResponse<List<NewsletterSubscriberDto>>.Ok(subscribers));
        }

        // GET: api/newslettersubscribers/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var sub = await _subscriberService.GetByIdAsync(id);
            if (sub == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy người đăng ký."));
            }
            return Ok(ApiResponse<NewsletterSubscriberDto>.Ok(sub));
        }

        // POST: api/newslettersubscribers/subscribe
        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Email không hợp lệ."));
            }

            var success = await _subscriberService.SubscribeAsync(request.Email);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Email này đã đăng ký trước đó hoặc có lỗi xảy ra."));
            }

            return Ok(ApiResponse<string>.Ok(request.Email, "Đăng ký nhận bản tin thành công!"));
        }

        // DELETE: api/newslettersubscribers/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _subscriberService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy người đăng ký để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa thành công."));
        }
    }
}

