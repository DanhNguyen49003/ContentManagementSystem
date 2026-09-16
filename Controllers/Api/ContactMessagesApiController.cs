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
    [Route("api/contactmessages")]
    [Route("api/contact-messages")]
    [ApiController]
    [Tags("ContactMessages")]
    public class ContactMessagesApiController : ControllerBase
    {
        private readonly IContactMessageService _contactMessageService;

        public ContactMessagesApiController(IContactMessageService contactMessageService)
        {
            _contactMessageService = contactMessageService;
        }

        // GET: api/contactmessages
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var messages = await _contactMessageService.GetAllAsync();
            return Ok(ApiResponse<List<ContactMessageDto>>.Ok(messages));
        }

        // GET: api/contactmessages/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var message = await _contactMessageService.GetByIdAsync(id);
            if (message == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy tin nhắn."));
            }
            return Ok(ApiResponse<ContactMessageDto>.Ok(message));
        }

        // POST: api/contactmessages
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContactMessageDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _contactMessageService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể gửi tin nhắn liên hệ."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<ContactMessageDto>.Ok(dto, "Gửi tin nhắn liên hệ thành công."));
        }

        // DELETE: api/contactmessages/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _contactMessageService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy tin nhắn để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa tin nhắn thành công."));
        }
    }
}

