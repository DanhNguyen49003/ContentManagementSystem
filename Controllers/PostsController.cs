using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContentManagementSystem.ApplicationCore.Entities;
using ContentManagementSystem.DataLayer;
using ContentManagementSystem.ApplicationCore.Interfaces;
using ContentManagementSystem.ApplicationCore.DTOs;

namespace ContentManagementSystem.Controllers
{
    public class PostsController : Controller
    {
        private readonly IPostService _postService;
        private readonly ContentManageDbContext _context; // For Categories dropdown
        private readonly ContentManageIdentityDbContext _identityContext; // For Users dropdown

        public PostsController(IPostService postService, ContentManageDbContext context, ContentManageIdentityDbContext identityContext)
        {
            _postService = postService;
            _context = context;
            _identityContext = identityContext;
        }

        // GET: Posts
        public async Task<IActionResult> Index()
        {
            var posts = await _postService.GetAllPostsAsync();
            return View(posts);
        }

        // GET: Posts/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var post = await _postService.GetPostByIdAsync(id.Value);
            if (post == null) return NotFound();

            return View(post);
        }

        // GET: Posts/Create
        public IActionResult Create()
        {
            ViewData["AuthorId"] = new SelectList(_identityContext.Users, "Id", "UserName");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Posts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Slug,Summary,Content,IsPublished,CategoryId,AuthorId")] PostDto postDto)
        {
            if (ModelState.IsValid)
            {
                await _postService.CreatePostAsync(postDto);
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_identityContext.Users, "Id", "UserName", postDto.AuthorId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", postDto.CategoryId);
            return View(postDto);
        }

        // GET: Posts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var postDto = await _postService.GetPostByIdAsync(id.Value);
            if (postDto == null) return NotFound();
            
            ViewData["AuthorId"] = new SelectList(_identityContext.Users, "Id", "UserName", postDto.AuthorId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", postDto.CategoryId);
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
                    await _postService.UpdatePostAsync(postDto);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_postService.PostExists(postDto.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_identityContext.Users, "Id", "UserName", postDto.AuthorId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", postDto.CategoryId);
            return View(postDto);
        }

        // GET: Posts/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var postDto = await _postService.GetPostByIdAsync(id.Value);
            if (postDto == null) return NotFound();

            return View(postDto);
        }

        // POST: Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _postService.DeletePostAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
