using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ContentManagementSystem.ApplicationCore.DTOs;
using ContentManagementSystem.ApplicationCore.Entities.Identity;
using ContentManagementSystem.Service.Interface;
using ContentManagementSystem.Services;
using ContentManagementSystem.Services.ApiClients;

namespace ContentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,QA Manager,QA Coordinator,Customer")]
    public class PostsController : Controller
    {
        private readonly IPostService _postService;
        private readonly ICategoryService _categoryService;
        private readonly IApiClient _apiClient;
        private readonly UserManager<ContentUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PostsController> _logger;

        public PostsController(
            IPostService postService,
            ICategoryService categoryService,
            IApiClient apiClient,
            UserManager<ContentUser> userManager,
            IEmailSender emailSender,
            ILogger<PostsController> logger)
        {
            _postService = postService;
            _categoryService = categoryService;
            _apiClient = apiClient;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        private async Task PopulateDropdownsAsync(string? selectedAuthorId = null, Guid? selectedCategoryId = null)
        {
            var categories = await _categoryService.GetAllAsync();
            var authors = await _apiClient.GetAsync<List<UserInfoDto>>("api/auth/authors") ?? new List<UserInfoDto>();

            if (authors.Count == 0 && User.Identity?.IsAuthenticated == true)
            {
                var currentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
                var currentName = User.Identity?.Name ?? "Tác giả hiện tại";
                authors.Add(new UserInfoDto { Id = currentId, FullName = currentName, Email = currentName });
            }

            ViewData["AuthorId"] = new SelectList(authors, "Id", "FullName", selectedAuthorId);
            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name", selectedCategoryId);
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
        [Authorize(Roles = "Admin,QA Manager,QA Coordinator")]
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
            await PopulateDropdownsAsync();
            return View();
        }

        // POST: Posts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Slug,Summary,Content,IsPublished,CategoryId,AuthorId")] PostDto postDto)
        {
            if (string.IsNullOrEmpty(postDto.AuthorId))
            {
                postDto.AuthorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            }

            if (ModelState.IsValid)
            {
                await _postService.CreateAsync(postDto);

                // Gửi email thông báo tới Ban Quản Trị và Ban Kiểm Duyệt QA để xem xét phê duyệt
                try
                {
                    var reviewers = new List<ContentUser>();
                    foreach (var role in new[] { "Admin", "QA Manager", "QA Coordinator" })
                    {
                        var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                        reviewers.AddRange(usersInRole);
                    }

                    var distinctReviewers = reviewers
                        .Where(u => !string.IsNullOrEmpty(u.Email))
                        .GroupBy(u => u.Email)
                        .Select(g => g.First())
                        .ToList();

                    var category = postDto.CategoryId != Guid.Empty ? await _categoryService.GetByIdAsync(postDto.CategoryId) : null;
                    var categoryName = category?.Name ?? "Chung";
                    var currentUserName = User.Identity?.Name ?? "Thành viên";
                    var reviewUrl = Url.Action("Index", "Posts", null, Request.Scheme) ?? "";

                    var emailHtml = EmailTemplateHelper.GenerateNewPostApprovalEmail(
                        postDto.Title,
                        currentUserName,
                        categoryName,
                        postDto.Summary ?? "",
                        reviewUrl);

                    foreach (var reviewer in distinctReviewers)
                    {
                        await _emailSender.SendEmailAsync(
                            reviewer.Email!,
                            $"[Cần duyệt] Bài viết mới: {postDto.Title}",
                            emailHtml);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi email thông báo duyệt bài viết {Title}", postDto.Title);
                }

                TempData["SuccessMessage"] = "Đăng bài viết thành công! Bài viết đã được chuyển tới Ban Kiểm Duyệt QA và Quản trị viên để xét duyệt.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(postDto.AuthorId, postDto.CategoryId);
            return View(postDto);
        }

        // GET: Posts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var postDto = await _postService.GetByIdAsync(id.Value);
            if (postDto == null) return NotFound();

            await PopulateDropdownsAsync(postDto.AuthorId, postDto.CategoryId);
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

            await PopulateDropdownsAsync(postDto.AuthorId, postDto.CategoryId);
            return View(postDto);
        }

        // GET: Posts/Delete/5
        [Authorize(Roles = "Admin,QA Manager")]
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
        [Authorize(Roles = "Admin,QA Manager")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _postService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

