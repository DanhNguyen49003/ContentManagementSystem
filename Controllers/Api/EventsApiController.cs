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
    [Route("api/events")]
    [ApiController]
    [Tags("Events")]
    public class EventsApiController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsApiController(IEventService eventService)
        {
            _eventService = eventService;
        }

        // GET: api/events
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var events = await _eventService.GetAllAsync();
            return Ok(ApiResponse<List<EventDto>>.Ok(events));
        }

        // GET: api/events/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var ev = await _eventService.GetByIdAsync(id);
            if (ev == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy sự kiện."));
            }
            return Ok(ApiResponse<EventDto>.Ok(ev));
        }

        // POST: api/events
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EventDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _eventService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể tạo sự kiện."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<EventDto>.Ok(dto, "Tạo sự kiện thành công."));
        }

        // PUT: api/events/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EventDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(ApiResponse<string>.Fail("Mã sự kiện không khớp."));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _eventService.UpdateAsync(dto);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy sự kiện để cập nhật."));
            }

            return Ok(ApiResponse<EventDto>.Ok(dto, "Cập nhật sự kiện thành công."));
        }

        // DELETE: api/events/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _eventService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy sự kiện để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa sự kiện thành công."));
        }
    }
}

