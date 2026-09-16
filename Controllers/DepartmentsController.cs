using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContentManagementSystem.ApplicationCore.DTOs;
using ContentManagementSystem.Service.Interface;

namespace ContentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,QA Manager")]
    public class DepartmentsController : Controller
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentsController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        // GET: /Departments
        public async Task<IActionResult> Index()
        {
            var departments = await _departmentService.GetAllAsync();
            return View(departments);
        }

        // GET: /Departments/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentDto dto)
        {
            if (ModelState.IsValid)
            {
                await _departmentService.CreateAsync(dto);
                TempData["SuccessMessage"] = "Thêm phòng ban mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }

        // GET: /Departments/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var dto = await _departmentService.GetByIdAsync(id.Value);
            if (dto == null) return NotFound();
            return View(dto);
        }

        // POST: /Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, DepartmentDto dto)
        {
            if (id != dto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _departmentService.UpdateAsync(dto);
                TempData["SuccessMessage"] = "Cập nhật thông tin phòng ban thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }

        // GET: /Departments/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            var dto = await _departmentService.GetByIdAsync(id.Value);
            if (dto == null) return NotFound();
            return View(dto);
        }

        // POST: /Departments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _departmentService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Xóa phòng ban thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}

