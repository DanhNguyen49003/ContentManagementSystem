using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ContentManagementSystem.ApplicationCore.DTOs;
using ContentManagementSystem.Service.Interface;


namespace ContentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _service;
        

        public CategoriesController(ICategoryService service)
        {
            _service = service;
            
        }

        public async Task<IActionResult> Index()
        {
            return View(await _service.GetAllAsync());
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();
            var dto = await _service.GetByIdAsync(id.Value);
            if (dto == null) return NotFound();
            return View(dto);
        }

        public IActionResult Create()
        {
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryDto dto)
        {
            if (ModelState.IsValid)
            {
                await _service.CreateAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            
            return View(dto);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var dto = await _service.GetByIdAsync(id.Value);
            if (dto == null) return NotFound();
            
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CategoryDto dto)
        {
            if (id != dto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _service.UpdateAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            
            return View(dto);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            var dto = await _service.GetByIdAsync(id.Value);
            if (dto == null) return NotFound();
            return View(dto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}


