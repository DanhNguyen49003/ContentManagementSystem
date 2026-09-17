using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContentManagementSystem.ApplicationCore.DTOs;
using ContentManagementSystem.Service.Interface;

namespace ContentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SubmissionWindowsController : Controller
    {
        private readonly ISubmissionWindowService _service;

        public SubmissionWindowsController(ISubmissionWindowService service)
        {
            _service = service;
        }

        // GET: SubmissionWindows
        public async Task<IActionResult> Index()
        {
            var list = await _service.GetAllAsync();
            return View(list);
        }

        // GET: SubmissionWindows/Create
        public IActionResult Create()
        {
            var model = new SubmissionWindowDto
            {
                StartDate = DateTime.UtcNow,
                ClosureDate = DateTime.UtcNow.AddDays(30),
                FinalClosureDate = DateTime.UtcNow.AddDays(45),
                IsActive = true
            };
            return View(model);
        }

        // POST: SubmissionWindows/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubmissionWindowDto dto)
        {
            if (dto.ClosureDate <= dto.StartDate)
            {
                ModelState.AddModelError("ClosureDate", "Hạn chót nộp bài (Closure Date) phải sau Ngày bắt đầu.");
            }
            if (dto.FinalClosureDate <= dto.ClosureDate)
            {
                ModelState.AddModelError("FinalClosureDate", "Hạn chót tương tác (Final Closure Date) phải sau Hạn chót nộp bài (Closure Date).");
            }

            if (ModelState.IsValid)
            {
                var success = await _service.CreateAsync(dto);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Đã tạo đợt nộp bài '{dto.Name}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, "Không thể lưu đợt nộp bài. Vui lòng thử lại.");
            }

            return View(dto);
        }

        // GET: SubmissionWindows/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var dto = await _service.GetByIdAsync(id.Value);
            if (dto == null) return NotFound();

            return View(dto);
        }

        // POST: SubmissionWindows/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, SubmissionWindowDto dto)
        {
            if (id != dto.Id) return NotFound();

            if (dto.ClosureDate <= dto.StartDate)
            {
                ModelState.AddModelError("ClosureDate", "Hạn chót nộp bài (Closure Date) phải sau Ngày bắt đầu.");
            }
            if (dto.FinalClosureDate <= dto.ClosureDate)
            {
                ModelState.AddModelError("FinalClosureDate", "Hạn chót tương tác (Final Closure Date) phải sau Hạn chót nộp bài (Closure Date).");
            }

            if (ModelState.IsValid)
            {
                var success = await _service.UpdateAsync(dto);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Cập nhật đợt nộp bài '{dto.Name}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, "Không thể cập nhật đợt nộp bài. Vui lòng thử lại.");
            }

            return View(dto);
        }

        // POST: SubmissionWindows/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            await _service.ToggleActiveAsync(id);
            TempData["SuccessMessage"] = "Đã thay đổi trạng thái kích hoạt của đợt nộp bài!";
            return RedirectToAction(nameof(Index));
        }

        // GET: SubmissionWindows/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var dto = await _service.GetByIdAsync(id.Value);
            if (dto == null) return NotFound();

            return View(dto);
        }

        // POST: SubmissionWindows/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _service.DeleteAsync(id);
            TempData["SuccessMessage"] = "Đã xóa đợt nộp bài thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}

