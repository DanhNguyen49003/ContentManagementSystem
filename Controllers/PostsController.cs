using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContentManagementSystem.ApplicationCore.DTOs;
using ContentManagementSystem.DataLayer;
using ContentManagementSystem.Service.Interface;

namespace ContentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Editor,Author")]
    public class PostsController : Controller
    {
        private readonly IPostService _postService;
        private readonly ICategoryService _categoryService;
        private readonly ContentManageIdentityDbContext _identityContext;

        public PostsController(
            IPostService postService,
            ICategoryService categoryService,
            ContentManageIdentityDbContext identityContext)
        {
            _postService = postService;
            _categoryService = categoryService;
            _identityContext = identityContext;
        }

        // GET: Posts
        public async Task<IActionResult> Index()
        {
            var posts = await _postService.GetAllAsync();
            return View(posts);
        }

        // POST: Posts/TogglePublish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> TogglePublish(Guid id)
        {
            await _postService.TogglePublishAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Posts/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var post = await _postService.GetByIdAsync(id.Value);
            if (post == null) return NotFound();

            return View(post);
        }

        // GET: Posts/Create
        public async Task<IActionResult> Create()
        {
            var categories = await _categoryService.GetAllAsync();
            ViewData["AuthorId"] = new SelectList(_identityContext.Users, "Id", "UserName");
            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name");
            return View();
        }

        // POST: Posts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Slug,Summary,Content,IsPublished,CategoryId,AuthorId")] PostDto postDto)
        {
            if (ModelState.IsValid)
            {
                await _postService.CreateAsync(postDto);
                return RedirectToAction(nameof(Index));
            }
            var categories = await _categoryService.GetAllAsync();
            ViewData["AuthorId"] = new SelectList(_identityContext.Users, "Id", "UserName", postDto.AuthorId);
            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name", postDto.CategoryId);
            return View(postDto);
        }

        // GET: Posts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var postDto = await _postService.GetByIdAsync(id.Value);
            if (postDto == null) return NotFound();

            var categories = await _categoryService.GetAllAsync();
            ViewData["AuthorId"] = new SelectList(_identityContext.Users, "Id", "UserName", postDto.AuthorId);
            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name", postDto.CategoryId);
            return View(postDto);
        }

        // POST: Posts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Slug,Summary,Content,IsPublished,CategoryId,AuthorId")] PostDto postDto)
        {
            if (id != postDto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _postService.UpdateAsync(postDto);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_postService.Exists(postDto.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            var categories = await _categoryService.GetAllAsync();
            ViewData["AuthorId"] = new SelectList(_identityContext.Users, "Id", "UserName", postDto.AuthorId);
            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name", postDto.CategoryId);
            return View(postDto);
        }

        // GET: Posts/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var postDto = await _postService.GetByIdAsync(id.Value);
            if (postDto == null) return NotFound();

            return View(postDto);
        }

        // POST: Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _postService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

