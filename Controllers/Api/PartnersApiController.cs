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
    [Route("api/partners")]
    [ApiController]
    [Tags("Partners")]
    public class PartnersApiController : ControllerBase
    {
        private readonly IPartnerService _partnerService;

        public PartnersApiController(IPartnerService partnerService)
        {
            _partnerService = partnerService;
        }

        // GET: api/partners
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var partners = await _partnerService.GetAllAsync();
            return Ok(ApiResponse<List<PartnerDto>>.Ok(partners));
        }

        // GET: api/partners/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var partner = await _partnerService.GetByIdAsync(id);
            if (partner == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy đối tác."));
            }
            return Ok(ApiResponse<PartnerDto>.Ok(partner));
        }

        // POST: api/partners
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PartnerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _partnerService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể tạo đối tác."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<PartnerDto>.Ok(dto, "Tạo đối tác thành công."));
        }

        // PUT: api/partners/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] PartnerDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(ApiResponse<string>.Fail("Mã đối tác không khớp."));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _partnerService.UpdateAsync(dto);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy đối tác để cập nhật."));
            }

            return Ok(ApiResponse<PartnerDto>.Ok(dto, "Cập nhật đối tác thành công."));
        }

        // DELETE: api/partners/{id}
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _partnerService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy đối tác để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa đối tác thành công."));
        }
    }
}

