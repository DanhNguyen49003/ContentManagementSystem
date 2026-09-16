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
        private readonly IDepartmentService _departmentService;
        private readonly ITagService _tagService;
        private readonly IApiClient _apiClient;
        private readonly UserManager<ContentUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PostsController> _logger;

        public PostsController(
            IPostService postService,
            ICategoryService categoryService,
            IDepartmentService departmentService,
            ITagService tagService,
            IApiClient apiClient,
            UserManager<ContentUser> userManager,
            IEmailSender emailSender,
            ILogger<PostsController> logger)
        {
            _postService = postService;
            _categoryService = categoryService;
            _departmentService = departmentService;
            _tagService = tagService;
            _apiClient = apiClient;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        private async Task PopulateDropdownsAsync(string? selectedAuthorId = null, Guid? selectedCategoryId = null, Guid? selectedDepartmentId = null)
        {
            var categories = await _categoryService.GetAllAsync();
            var departments = await _departmentService.GetAllAsync();
            var authors = await _apiClient.GetAsync<List<UserInfoDto>>("api/auth/authors") ?? new List<UserInfoDto>();
            var tags = await _tagService.GetAllAsync();

            if (tags == null || tags.Count == 0)
            {
                var defaultTags = new[] { "Công nghệ", "Tin tức", "Học tập", "Lập trình", "AI", "Kinh doanh" };
                foreach (var tagName in defaultTags)
                {
                    await _tagService.CreateAsync(new TagDto { Name = tagName });
                }
                tags = await _tagService.GetAllAsync();
            }

            if (authors.Count == 0 && User.Identity?.IsAuthenticated == true)
            {
                var currentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
                var currentName = User.Identity?.Name ?? "Tác giả hiện tại";
                authors.Add(new UserInfoDto { Id = currentId, FullName = currentName, Email = currentName });
            }

            ViewData["AuthorId"] = new SelectList(authors, "Id", "FullName", selectedAuthorId);
            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name", selectedCategoryId);
            ViewData["DepartmentId"] = new SelectList(departments, "Id", "Name", selectedDepartmentId);
            ViewBag.AvailableTags = tags;
        }

        // GET: Posts
        public async Task<IActionResult> Index()
        {
            var posts = await _postService.GetAllAsync();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;
            ViewBag.UserDepartmentId = currentUser?.DepartmentId;

            return View(posts);
        }

        // POST: Posts/TogglePublish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,QA Coordinator")]
        public async Task<IActionResult> TogglePublish(Guid id)
        {
            var post = await _postService.GetByIdAsync(id);
            if (post == null) return NotFound();

            // Nếu người duyệt là QA Coordinator -> Kiểm tra bắt buộc phải cùng phòng ban với bài viết
            if (User.IsInRole("QA Coordinator") && !User.IsInRole("Admin"))
            {
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;

                if (currentUser?.DepartmentId == null || post.DepartmentId != currentUser.DepartmentId)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền duyệt bài viết này vì bài viết không thuộc phòng ban phụ trách của bạn!";
                    return RedirectToAction(nameof(Index));
                }
            }

            await _postService.TogglePublishAsync(id);
            TempData["SuccessMessage"] = "Cập nhật trạng thái duyệt bài viết thành công!";
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
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;
            await PopulateDropdownsAsync(selectedAuthorId: currentUserId, selectedDepartmentId: currentUser?.DepartmentId);
            return View();
        }

        // POST: Posts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Slug,Summary,Content,IsPublished,CategoryId,DepartmentId,AuthorId,SelectedTagIds,CustomTags")] PostDto postDto)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;

            if (string.IsNullOrEmpty(postDto.AuthorId))
            {
                postDto.AuthorId = currentUserId;
            }

            if (postDto.DepartmentId == null && currentUser?.DepartmentId != null)
            {
                postDto.DepartmentId = currentUser.DepartmentId;
            }

            if (ModelState.IsValid)
            {
                await _postService.CreateAsync(postDto);

                // Gửi email thông báo tới đúng QA Coordinator của phòng ban đó và Admin
                try
                {
                    var reviewers = new List<ContentUser>();
                    
                    // 1. QA Coordinator của chính phòng ban này
                    var coordinators = await _userManager.GetUsersInRoleAsync("QA Coordinator");
                    if (postDto.DepartmentId.HasValue)
                    {
                        reviewers.AddRange(coordinators.Where(c => c.DepartmentId == postDto.DepartmentId.Value));
                    }
                    else
                    {
                        reviewers.AddRange(coordinators);
                    }

                    // 2. Admin hệ thống
                    var admins = await _userManager.GetUsersInRoleAsync("Admin");
                    reviewers.AddRange(admins);

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

                TempData["SuccessMessage"] = "Đăng bài viết thành công! Bài viết đã được chuyển tới QA Coordinator của phòng ban để xét duyệt.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(postDto.AuthorId, postDto.CategoryId, postDto.DepartmentId);
            return View(postDto);
        }

        // GET: Posts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var postDto = await _postService.GetByIdAsync(id.Value);
            if (postDto == null) return NotFound();

            await PopulateDropdownsAsync(postDto.AuthorId, postDto.CategoryId, postDto.DepartmentId);
            return View(postDto);
        }

        // POST: Posts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Slug,Summary,Content,IsPublished,CategoryId,DepartmentId,AuthorId,SelectedTagIds,CustomTags")] PostDto postDto)
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

            await PopulateDropdownsAsync(postDto.AuthorId, postDto.CategoryId, postDto.DepartmentId);
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

