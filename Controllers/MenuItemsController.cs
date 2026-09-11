using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContentManagementSystem.ApplicationCore.Entities;
using ContentManagementSystem.ApplicationCore.Interfaces;
using ContentManagementSystem.DataLayer;

namespace ContentManagementSystem.Controllers
{
    public class MenuItemsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public MenuItemsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: MenuItems
        public async Task<IActionResult> Index()
        {
            var contentManageDbContext = _unitOfWork.MenuItems.AsQueryable().Include(m => m.Menu);
            return View(await contentManageDbContext.ToListAsync());
        }

        // GET: MenuItems/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _unitOfWork.MenuItems.AsQueryable()
                .Include(m => m.Menu)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }

        // GET: MenuItems/Create
        public IActionResult Create()
        {
            ViewData["MenuId"] = new SelectList(_unitOfWork.Menus.AsQueryable(), "Id", "Name");
            return View();
        }

        // POST: MenuItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Url,MenuId")] MenuItem menuItem)
        {
            if (ModelState.IsValid)
            {
                menuItem.Id = Guid.NewGuid();
                _unitOfWork.MenuItems.Add(menuItem);
                await _unitOfWork.CompleteAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MenuId"] = new SelectList(_unitOfWork.Menus.AsQueryable(), "Id", "Name", menuItem.MenuId);
            return View(menuItem);
        }

        // GET: MenuItems/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(id);
            if (menuItem == null)
            {
                return NotFound();
            }
            ViewData["MenuId"] = new SelectList(_unitOfWork.Menus.AsQueryable(), "Id", "Name", menuItem.MenuId);
            return View(menuItem);
        }

        // POST: MenuItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Url,MenuId")] MenuItem menuItem)
        {
            if (id != menuItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _unitOfWork.MenuItems.Update(menuItem);
                    await _unitOfWork.CompleteAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MenuItemExists(menuItem.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MenuId"] = new SelectList(_unitOfWork.Menus.AsQueryable(), "Id", "Name", menuItem.MenuId);
            return View(menuItem);
        }

        // GET: MenuItems/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _unitOfWork.MenuItems.AsQueryable()
                .Include(m => m.Menu)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }

        // POST: MenuItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(id);
            if (menuItem != null)
            {
                _unitOfWork.MenuItems.Remove(menuItem);
            }

            await _unitOfWork.CompleteAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MenuItemExists(Guid id)
        {
            return _unitOfWork.MenuItems.Any(e => e.Id == id);
        }
    }
}


