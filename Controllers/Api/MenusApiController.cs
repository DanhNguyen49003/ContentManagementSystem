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
    [Route("api/menus")]
    [ApiController]
    [Tags("Menus")]
    public class MenusApiController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenusApiController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        // GET: api/menus
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var menus = await _menuService.GetAllAsync();
            return Ok(ApiResponse<List<MenuDto>>.Ok(menus));
        }

        // GET: api/menus/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var menu = await _menuService.GetByIdAsync(id);
            if (menu == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy menu."));
            }
            return Ok(ApiResponse<MenuDto>.Ok(menu));
        }

        // POST: api/menus
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MenuDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _menuService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể tạo menu."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<MenuDto>.Ok(dto, "Tạo menu thành công."));
        }

        // PUT: api/menus/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] MenuDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(ApiResponse<string>.Fail("Mã menu không khớp."));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _menuService.UpdateAsync(dto);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy menu để cập nhật."));
            }

            return Ok(ApiResponse<MenuDto>.Ok(dto, "Cập nhật menu thành công."));
        }

        // DELETE: api/menus/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _menuService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy menu để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa menu thành công."));
        }
    }
}

