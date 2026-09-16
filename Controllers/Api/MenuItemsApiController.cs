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
    [Route("api/menuitems")]
    [Route("api/menu-items")]
    [ApiController]
    [Tags("MenuItems")]
    public class MenuItemsApiController : ControllerBase
    {
        private readonly IMenuItemService _menuItemService;

        public MenuItemsApiController(IMenuItemService menuItemService)
        {
            _menuItemService = menuItemService;
        }

        // GET: api/menuitems
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _menuItemService.GetAllAsync();
            return Ok(ApiResponse<List<MenuItemDto>>.Ok(items));
        }

        // GET: api/menuitems/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var item = await _menuItemService.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy mục menu."));
            }
            return Ok(ApiResponse<MenuItemDto>.Ok(item));
        }

        // POST: api/menuitems
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MenuItemDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _menuItemService.CreateAsync(dto);
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail("Không thể tạo mục menu."));
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<MenuItemDto>.Ok(dto, "Tạo mục menu thành công."));
        }

        // PUT: api/menuitems/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] MenuItemDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(ApiResponse<string>.Fail("Mã mục menu không khớp."));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.Fail("Dữ liệu không hợp lệ."));
            }

            var success = await _menuItemService.UpdateAsync(dto);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy mục menu để cập nhật."));
            }

            return Ok(ApiResponse<MenuItemDto>.Ok(dto, "Cập nhật mục menu thành công."));
        }

        // DELETE: api/menuitems/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _menuItemService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail("Không tìm thấy mục menu để xóa."));
            }

            return Ok(ApiResponse<string>.Ok(id.ToString(), "Xóa mục menu thành công."));
        }
    }
}

