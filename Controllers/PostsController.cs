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
    public class PostsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ContentManageIdentityDbContext _identityContext;

        public PostsController(IUnitOfWork unitOfWork, ContentManageIdentityDbContext identityContext)
        {
            _unitOfWork = unitOfWork;
            _identityContext = identityContext;
        }

        // GET: Posts
        public async Task<IActionResult> Index()
        {
            var contentManageDbContext = _unitOfWork.Posts.AsQueryable().Include(p => p.Author).Include(p => p.Category);
            return View(await contentManageDbContext.ToListAsync());
        }

        // GET: Posts/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _unitOfWork.Posts.AsQueryable()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // GET: Posts/Create
        public IActionResult Create()
        {
            ViewData["AuthorId"] = new SelectList(_identityContext.Users, "Id", "Email");
            ViewData["CategoryId"] = new SelectList(_unitOfWork.Categories.AsQueryable(), "Id", "Description");
            return View();
        }

        // POST: Posts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Slug,Content,Summary,IsPublished,CreatedAt,PublishedAt,AuthorId,CategoryId")] Post post)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Posts.Add(post);
                await _unitOfWork.CompleteAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_identityContext.Users, "Id", "Email", post.AuthorId);
            ViewData["CategoryId"] = new SelectList(_unitOfWork.Categories.AsQueryable(), "Id", "Description", post.CategoryId);
            return View(post);
        }

        // GET: Posts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _unitOfWork.Posts.GetByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }
            ViewData["AuthorId"] = new SelectList(_identityContext.Users, "Id", "Email", post.AuthorId);
            ViewData["CategoryId"] = new SelectList(_unitOfWork.Categories.AsQueryable(), "Id", "Description", post.CategoryId);
            return View(post);
        }

        // POST: Posts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Slug,Content,Summary,IsPublished,CreatedAt,PublishedAt,AuthorId,CategoryId")] Post post)
        {
            if (id != post.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _unitOfWork.Posts.Update(post);
                    await _unitOfWork.CompleteAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.Id))
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
            ViewData["AuthorId"] = new SelectList(_identityContext.Users, "Id", "Email", post.AuthorId);
            ViewData["CategoryId"] = new SelectList(_unitOfWork.Categories.AsQueryable(), "Id", "Description", post.CategoryId);
            return View(post);
        }

        // GET: Posts/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _unitOfWork.Posts.AsQueryable()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // POST: Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(id);
            if (post != null)
            {
                _unitOfWork.Posts.Remove(post);
            }

            await _unitOfWork.CompleteAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(Guid id)
        {
            return _unitOfWork.Posts.Any(e => e.Id == id);
        }
    }
}




