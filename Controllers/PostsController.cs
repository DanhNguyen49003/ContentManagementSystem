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
using Microsoft.Extensions.Caching.Memory;
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
        private const string HomePageDashboardCacheKey = "CMS_HOMEPAGE_DASHBOARD_DATA";

        private readonly IPostService _postService;
        private readonly ICategoryService _categoryService;
        private readonly IDepartmentService _departmentService;
        private readonly ITagService _tagService;
        private readonly ISubmissionWindowService _submissionWindowService;
        private readonly IApiClient _apiClient;
        private readonly UserManager<ContentUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PostsController> _logger;
        private readonly IMemoryCache _cache;

        public PostsController(
            IPostService postService,
            ICategoryService categoryService,
            IDepartmentService departmentService,
            ITagService tagService,
            ISubmissionWindowService submissionWindowService,
            IApiClient apiClient,
            UserManager<ContentUser> userManager,
            IEmailSender emailSender,
            ILogger<PostsController> logger,
            IMemoryCache cache)
        {
            _postService = postService;
            _categoryService = categoryService;
            _departmentService = departmentService;
            _tagService = tagService;
            _submissionWindowService = submissionWindowService;
            _apiClient = apiClient;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _cache = cache;
        }

        private void InvalidateDashboardCache()
        {
            try
            {
                _cache.Remove(HomePageDashboardCacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not invalidate homepage dashboard cache.");
            }
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
        public async Task<IActionResult> Index(Guid? departmentId = null)
        {
            var posts = await _postService.GetAllAsync();
            var departments = await _departmentService.GetAllAsync();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;

            // Người dùng có vai trò Customer chỉ thấy bài viết ĐÃ ĐƯỢC DUYỆT (Đã xuất bản)
            if (User.IsInRole("Customer") && !User.IsInRole("Admin") && !User.IsInRole("QA Coordinator") && !User.IsInRole("QA Manager"))
            {
                posts = posts.Where(p => p.IsPublished).ToList();
            }

            if (departmentId.HasValue)
            {
                posts = posts.Where(p => p.DepartmentId == departmentId.Value).ToList();
            }

            ViewBag.UserDepartmentId = currentUser?.DepartmentId;
            ViewBag.Departments = departments;
            ViewBag.SelectedDepartmentId = departmentId;

            return View(posts);
        }

        // GET: Posts/MyPosts - Trang quản lý bài viết riêng của người dùng đăng nhập
        public async Task<IActionResult> MyPosts(string? status = null, string? search = null)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUserName = User.Identity?.Name;
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;

            var myPosts = !string.IsNullOrEmpty(currentUserId) 
                ? await _postService.GetByAuthorIdAsync(currentUserId) 
                : new List<PostDto>();

            // Thống kê số lượng theo từng trạng thái
            ViewBag.CountAll = myPosts.Count;
            ViewBag.CountPending = myPosts.Count(p => (p.Status == "Pending" || string.IsNullOrEmpty(p.Status)) && !p.IsPublished);
            ViewBag.CountChanges = myPosts.Count(p => p.Status == "ChangesRequested");
            ViewBag.CountApproved = myPosts.Count(p => p.IsPublished);
            ViewBag.CountRejected = myPosts.Count(p => p.Status == "Rejected");

            // Lọc theo trạng thái
            IEnumerable<PostDto> filtered = myPosts;
            if (status == "Pending")
                filtered = filtered.Where(p => (p.Status == "Pending" || string.IsNullOrEmpty(p.Status)) && !p.IsPublished);
            else if (status == "Changes")
                filtered = filtered.Where(p => p.Status == "ChangesRequested");
            else if (status == "Approved")
                filtered = filtered.Where(p => p.IsPublished);
            else if (status == "Rejected")
                filtered = filtered.Where(p => p.Status == "Rejected");

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                filtered = filtered.Where(p => (!string.IsNullOrEmpty(p.Title) && p.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
                                            || (!string.IsNullOrEmpty(p.Summary) && p.Summary.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            ViewBag.CurrentStatus = status;
            ViewBag.SearchTerm = search;
            ViewBag.UserDepartmentId = currentUser?.DepartmentId;
            if (currentUser?.DepartmentId != null)
            {
                var dept = await _departmentService.GetByIdAsync(currentUser.DepartmentId.Value);
                ViewBag.UserDepartmentName = dept?.Name;
            }
            ViewBag.ActiveWindow = await _submissionWindowService.GetActiveWindowAsync();

            return View(filtered.OrderByDescending(p => p.CreatedAt).ToList());
        }

        // POST: Posts/DeleteMyPost/5 - Cho phép tác giả rút/xóa bài viết của chính mình nếu chưa duyệt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMyPost(Guid id)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUserName = User.Identity?.Name;
            var post = await _postService.GetByIdAsync(id);
            if (post == null) return NotFound();

            bool isAuthor = (!string.IsNullOrEmpty(currentUserId) && string.Equals(post.AuthorId, currentUserId, StringComparison.OrdinalIgnoreCase))
                         || (!string.IsNullOrEmpty(currentUserName) && string.Equals(post.AuthorName, currentUserName, StringComparison.OrdinalIgnoreCase));

            if (!isAuthor && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa bài viết này!";
                return RedirectToAction(nameof(MyPosts));
            }

            if (post.IsPublished && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Không thể rút/xóa bài viết đã được duyệt và xuất bản! Vui lòng liên hệ QA Coordinator hoặc Admin nếu bạn có nhu cầu gỡ bài.";
                return RedirectToAction(nameof(MyPosts));
            }

            await _postService.DeleteAsync(id);
            InvalidateDashboardCache();
            TempData["SuccessMessage"] = $"Đã rút bài viết '{post.Title}' thành công!";
            return RedirectToAction(nameof(MyPosts));
        }

        // GET: Posts/ExportCsv
        [Authorize(Roles = "Admin,QA Manager")]
        public async Task<IActionResult> ExportCsv(Guid? departmentId = null)
        {
            var posts = await _postService.GetAllAsync();
            if (departmentId.HasValue)
                posts = posts.Where(p => p.DepartmentId == departmentId.Value).ToList();

            var sb = new System.Text.StringBuilder();
            // UTF-8 BOM header cho Excel mở được tiếng Việt
            sb.AppendLine("ID,Tiêu đề,Chuyên mục,Phòng ban,Tác giả,Ẩn danh,Trạng thái,Đợt nộp bài,Tags,Ngày tạo,Ngày duyệt,Người duyệt,Phản hồi");

            foreach (var p in posts)
            {
                string Esc(string? s) => $"\"{(s ?? "").Replace("\"", "\"\"")}\"";
                var status = p.IsPublished ? "Đã duyệt" :
                             p.Status == "Rejected" ? "Từ chối" :
                             p.Status == "ChangesRequested" ? "Cần sửa" : "Chờ duyệt";
                var tags = string.Join(";", p.TagNames ?? new List<string>());
                sb.AppendLine(string.Join(",",
                    Esc(p.Id.ToString()),
                    Esc(p.Title),
                    Esc(p.CategoryName),
                    Esc(p.DepartmentName),
                    Esc(p.IsAnonymous ? "Ẩn danh" : p.AuthorName),
                    Esc(p.IsAnonymous ? "Có" : "Không"),
                    Esc(status),
                    Esc(p.SubmissionWindowName),
                    Esc(tags),
                    Esc(p.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")),
                    Esc(p.ReviewedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? ""),
                    Esc(p.ReviewedByName),
                    Esc(p.ReviewerFeedback)
                ));
            }

            // UTF-8 BOM bytes prefix
            var bom = System.Text.Encoding.UTF8.GetPreamble();
            var content = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var csvBytes = bom.Concat(content).ToArray();

            var filename = $"BaiViet_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(csvBytes, "text/csv; charset=utf-8", filename);
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

            bool willPublish = !post.IsPublished;
            await _postService.TogglePublishAsync(id);
            InvalidateDashboardCache();

            if (willPublish)
            {
                TempData["SuccessMessage"] = $"Đã duyệt và xuất bản bài viết '{post.Title}' thành công!";

                // Gửi email thông báo tự động cho tác giả bài viết
                try
                {
                    if (!string.IsNullOrEmpty(post.AuthorId))
                    {
                        var author = await _userManager.FindByIdAsync(post.AuthorId);
                        if (author != null && !string.IsNullOrEmpty(author.Email))
                        {
                            var postUrl = Url.Action("Details", "Posts", new { id = post.Id }, Request.Scheme) ?? "";
                            var emailHtml = EmailTemplateHelper.GeneratePostApprovedEmail(post.Title, author.FullName ?? author.UserName ?? "", postUrl);
                            await _emailSender.SendEmailAsync(author.Email, $"[CMS Portal] Bài viết '{post.Title}' của bạn đã được duyệt và đăng tải!", emailHtml);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi gửi email chúc mừng duyệt bài cho tác giả {AuthorId}", post.AuthorId);
                }
            }
            else
            {
                TempData["SuccessMessage"] = $"Đã thu hồi bài viết '{post.Title}' về trạng thái Bản nháp (Chờ duyệt).";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Posts/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var post = await _postService.GetByIdAsync(id.Value);
            if (post == null) return NotFound();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;
            bool isAdmin = User.IsInRole("Admin");
            bool isCoordinator = User.IsInRole("QA Coordinator");
            bool isAuthor = !string.IsNullOrEmpty(currentUserId) && post.AuthorId == currentUserId;
            bool canApprove = isAdmin || (isCoordinator && currentUser?.DepartmentId != null && post.DepartmentId != null && currentUser.DepartmentId == post.DepartmentId);

            // Nếu bài viết chưa xuất bản (chờ duyệt): Chỉ người có quyền duyệt hoặc chính tác giả mới được xem
            if (!post.IsPublished && !canApprove && !isAuthor && !User.IsInRole("QA Manager"))
            {
                TempData["ErrorMessage"] = "Bài viết này đang ở trạng thái 'Chờ duyệt' và chưa được xuất bản ra bên ngoài.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CanApprove = canApprove;
            return View(post);
        }

        // GET: Posts/Create
        public async Task<IActionResult> Create()
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;
            await PopulateDropdownsAsync(selectedAuthorId: currentUserId, selectedDepartmentId: currentUser?.DepartmentId);

            var activeWindow = await _submissionWindowService.GetActiveWindowAsync();
            var now = DateTime.UtcNow;
            bool isSubmissionClosed = false;

            if (!User.IsInRole("Admin"))
            {
                if (activeWindow == null || now < activeWindow.StartDate || now > activeWindow.ClosureDate)
                {
                    isSubmissionClosed = true;
                }
            }

            // Ràng buộc phòng ban: Tác giả/nhân viên chỉ được chọn phòng ban của mình, không chọn được phòng khác
            bool isAdmin = User.IsInRole("Admin");
            if (!isAdmin)
            {
                if (currentUser?.DepartmentId != null)
                {
                    var dept = await _departmentService.GetByIdAsync(currentUser.DepartmentId.Value);
                    ViewBag.LockedDepartmentId = currentUser.DepartmentId;
                    ViewBag.LockedDepartmentName = dept?.Name ?? "Phòng ban của bạn";
                    ViewBag.IsDepartmentLocked = true;
                }
                else
                {
                    ViewBag.UserHasNoDepartment = true;
                    ViewBag.IsDepartmentLocked = true;
                }
            }
            else
            {
                ViewBag.IsDepartmentLocked = false;
            }

            ViewBag.ActiveWindow = activeWindow;
            ViewBag.IsSubmissionClosed = isSubmissionClosed;
            return View();
        }

        // POST: Posts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Slug,Summary,Content,IsPublished,IsAnonymous,CategoryId,DepartmentId,SelectedTagIds,CustomTags")] PostDto postDto)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;
            bool isAdmin = User.IsInRole("Admin");

            // Người tạo bài viết luôn luôn là tác giả của bài viết
            postDto.AuthorId = currentUserId;

            // Ràng buộc phòng ban: Nếu không phải Admin thì BẮT BUỘC theo phòng ban của tác giả
            if (!isAdmin)
            {
                if (currentUser?.DepartmentId == null)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn chưa được gán vào phòng ban nào. Vui lòng liên hệ Quản trị viên để được xếp phòng ban trước khi đăng bài.");
                    ViewBag.UserHasNoDepartment = true;
                    ViewBag.IsDepartmentLocked = true;
                }
                else
                {
                    postDto.DepartmentId = currentUser.DepartmentId;
                    var dept = await _departmentService.GetByIdAsync(currentUser.DepartmentId.Value);
                    ViewBag.LockedDepartmentId = currentUser.DepartmentId;
                    ViewBag.LockedDepartmentName = dept?.Name ?? "Phòng ban của bạn";
                    ViewBag.IsDepartmentLocked = true;
                }
            }
            else
            {
                ViewBag.IsDepartmentLocked = false;
            }

            // BẮT BUỘC DUYỆT: Mọi bài viết tạo mới đều bắt đầu ở trạng thái Chờ duyệt (IsPublished = false)
            // Chỉ Admin mới có quyền tự động xuất bản ngay nếu chọn
            if (!isAdmin)
            {
                postDto.IsPublished = false;

                // Time-boxing: Kiểm tra đợt nộp bài
                var activeWindow = await _submissionWindowService.GetActiveWindowAsync();
                var now = DateTime.UtcNow;
                if (activeWindow == null || now < activeWindow.StartDate || now > activeWindow.ClosureDate)
                {
                    ModelState.AddModelError(string.Empty, "Đợt nộp bài hiện tại đã đóng (quá hạn chót nộp bài). Bạn không thể tạo bài viết mới.");
                    await PopulateDropdownsAsync(postDto.AuthorId, postDto.CategoryId, postDto.DepartmentId);
                    ViewBag.ActiveWindow = activeWindow;
                    ViewBag.IsSubmissionClosed = true;
                    return View(postDto);
                }
            }

            if (ModelState.IsValid)
            {
                await _postService.CreateAsync(postDto);
                InvalidateDashboardCache();

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
                return RedirectToAction(nameof(MyPosts));
            }

            var window = await _submissionWindowService.GetActiveWindowAsync();
            ViewBag.ActiveWindow = window;
            ViewBag.IsSubmissionClosed = !isAdmin && (window == null || DateTime.UtcNow > window.ClosureDate);
            await PopulateDropdownsAsync(postDto.AuthorId, postDto.CategoryId, postDto.DepartmentId);
            return View(postDto);
        }

        // GET: Posts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var postDto = await _postService.GetByIdAsync(id.Value);
            if (postDto == null) return NotFound();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;
            bool isAdmin = User.IsInRole("Admin");
            bool isManager = User.IsInRole("QA Manager");
            bool isCoordinator = User.IsInRole("QA Coordinator");
            var currentUserName = User.Identity?.Name;
            bool isAuthor = (!string.IsNullOrEmpty(currentUserId) && string.Equals(postDto.AuthorId, currentUserId, StringComparison.OrdinalIgnoreCase))
                         || (!string.IsNullOrEmpty(currentUserName) && string.Equals(postDto.AuthorName, currentUserName, StringComparison.OrdinalIgnoreCase));

            // Customer chỉ được sửa bài của chính mình
            if (User.IsInRole("Customer") && !isAdmin && !isManager && !isCoordinator && !isAuthor)
            {
                TempData["ErrorMessage"] = "Bạn chỉ có thể chỉnh sửa bài viết do chính bạn tạo!";
                return RedirectToAction(nameof(Index));
            }

            // Time-boxing: Tác giả không thể sửa bài nếu đợt nộp bài đã qua ClosureDate
            if (isAuthor && !isAdmin && !isManager && !isCoordinator)
            {
                if (!postDto.IsSubmissionOpen)
                {
                    TempData["ErrorMessage"] = "Đợt nộp bài của bài viết này đã kết thúc hạn nộp (Closure Date). Bạn không thể chỉnh sửa bài viết nữa.";
                    return RedirectToAction(nameof(Details), new { id = postDto.Id });
                }
            }

            bool canApprove = isAdmin || (isCoordinator && currentUser?.DepartmentId != null && postDto.DepartmentId != null && currentUser.DepartmentId == postDto.DepartmentId);
            ViewBag.CanApprove = canApprove;

            // Ràng buộc phòng ban khi sửa bài: Nếu không phải Admin thì cố định phòng ban của bài viết
            if (!isAdmin)
            {
                var lockedDeptId = postDto.DepartmentId ?? currentUser?.DepartmentId;
                if (lockedDeptId != null)
                {
                    var dept = await _departmentService.GetByIdAsync(lockedDeptId.Value);
                    ViewBag.LockedDepartmentId = lockedDeptId;
                    ViewBag.LockedDepartmentName = dept?.Name ?? postDto.DepartmentName ?? "Phòng ban của bạn";
                    ViewBag.IsDepartmentLocked = true;
                }
                else
                {
                    ViewBag.UserHasNoDepartment = true;
                    ViewBag.IsDepartmentLocked = true;
                }
            }
            else
            {
                ViewBag.IsDepartmentLocked = false;
            }

            await PopulateDropdownsAsync(postDto.AuthorId, postDto.CategoryId, postDto.DepartmentId);
            return View(postDto);
        }

        // POST: Posts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Slug,Summary,Content,IsPublished,IsAnonymous,CategoryId,DepartmentId,AuthorId,SelectedTagIds,CustomTags")] PostDto postDto)
        {
            if (id != postDto.Id) return NotFound();

            var originalPost = await _postService.GetByIdAsync(id);
            if (originalPost == null) return NotFound();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;
            bool isAdmin = User.IsInRole("Admin");
            bool isManager = User.IsInRole("QA Manager");
            bool isCoordinator = User.IsInRole("QA Coordinator");
            var currentUserName = User.Identity?.Name;
            bool isAuthor = (!string.IsNullOrEmpty(currentUserId) && string.Equals(originalPost.AuthorId, currentUserId, StringComparison.OrdinalIgnoreCase))
                         || (!string.IsNullOrEmpty(currentUserName) && string.Equals(originalPost.AuthorName, currentUserName, StringComparison.OrdinalIgnoreCase));

            // Customer chỉ được sửa bài của chính mình
            if (User.IsInRole("Customer") && !isAdmin && !isManager && !isCoordinator && !isAuthor)
            {
                TempData["ErrorMessage"] = "Bạn chỉ có thể chỉnh sửa bài viết do chính bạn tạo!";
                return RedirectToAction(nameof(Index));
            }

            // Time-boxing: Tác giả không thể lưu chỉnh sửa nếu đợt nộp bài đã đóng
            if (isAuthor && !isAdmin && !isManager && !isCoordinator)
            {
                if (!originalPost.IsSubmissionOpen)
                {
                    TempData["ErrorMessage"] = "Đợt nộp bài của bài viết này đã kết thúc hạn nộp (Closure Date). Không thể lưu chỉnh sửa.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            bool canApprove = isAdmin || (isCoordinator && currentUser?.DepartmentId != null && originalPost.DepartmentId != null && currentUser.DepartmentId == originalPost.DepartmentId);

            // Nếu người sửa không có quyền duyệt: không cho phép tự ý xuất bản bài viết
            if (!canApprove)
            {
                postDto.IsPublished = originalPost.IsPublished;
            }

            // Tác giả bài viết không thể bị thay đổi khi chỉnh sửa
            postDto.AuthorId = originalPost.AuthorId;

            // Ràng buộc phòng ban: nếu không phải Admin thì cố định phòng ban của bài viết
            if (!isAdmin)
            {
                postDto.DepartmentId = originalPost.DepartmentId ?? currentUser?.DepartmentId;
                var dept = postDto.DepartmentId.HasValue ? await _departmentService.GetByIdAsync(postDto.DepartmentId.Value) : null;
                ViewBag.LockedDepartmentId = postDto.DepartmentId;
                ViewBag.LockedDepartmentName = dept?.Name ?? originalPost.DepartmentName ?? "Phòng ban của bạn";
                ViewBag.IsDepartmentLocked = true;
            }
            else
            {
                ViewBag.IsDepartmentLocked = false;
            }

            // Vòng đời: Khi tác giả sửa bài viết bị "ChangesRequested", tự động chuyển về "Pending" để QA duyệt lại
            if (originalPost.Status == "ChangesRequested" && isAuthor && !canApprove)
            {
                postDto.Status = "Pending";
                postDto.IsPublished = false;
            }
            else
            {
                postDto.Status = originalPost.Status;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _postService.UpdateAsync(postDto);
                    InvalidateDashboardCache();
                    TempData["SuccessMessage"] = originalPost.Status == "ChangesRequested" && isAuthor
                        ? "Đã cập nhật bài viết và nộp lại cho QA Coordinator xét duyệt!"
                        : "Cập nhật bài viết thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_postService.Exists(postDto.Id)) return NotFound();
                    else throw;
                }
                return isAuthor && !isAdmin ? RedirectToAction(nameof(MyPosts)) : RedirectToAction(nameof(Index));
            }

            ViewBag.CanApprove = canApprove;
            await PopulateDropdownsAsync(postDto.AuthorId, postDto.CategoryId, postDto.DepartmentId);
            return View(postDto);
        }

        // POST: Posts/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,QA Coordinator")]
        public async Task<IActionResult> Approve(Guid id)
        {
            var post = await _postService.GetByIdAsync(id);
            if (post == null) return NotFound();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;

            if (User.IsInRole("QA Coordinator") && !User.IsInRole("Admin"))
            {
                if (currentUser?.DepartmentId == null || post.DepartmentId != currentUser.DepartmentId)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền duyệt bài viết này vì bài viết không thuộc phòng ban phụ trách của bạn!";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            await _postService.ApproveAsync(id, currentUserId);
            InvalidateDashboardCache();

            TempData["SuccessMessage"] = $"✅ Đã duyệt và xuất bản bài viết '{post.Title}' thành công!";

            // Email notify author
            try
            {
                if (!string.IsNullOrEmpty(post.AuthorId))
                {
                    var author = await _userManager.FindByIdAsync(post.AuthorId);
                    if (author != null && !string.IsNullOrEmpty(author.Email))
                    {
                        var postUrl = Url.Action("Details", "Posts", new { id = post.Id }, Request.Scheme) ?? "";
                        var emailHtml = EmailTemplateHelper.GeneratePostApprovedEmail(post.Title, author.FullName ?? author.UserName ?? "", postUrl);
                        await _emailSender.SendEmailAsync(author.Email, $"[CMS Portal] Bài viết '{post.Title}' của bạn đã được duyệt và đăng tải!", emailHtml);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gửi email thông báo duyệt bài {PostId}", id);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Posts/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,QA Coordinator")]
        public async Task<IActionResult> Reject(Guid id, string feedback)
        {
            var post = await _postService.GetByIdAsync(id);
            if (post == null) return NotFound();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;

            if (User.IsInRole("QA Coordinator") && !User.IsInRole("Admin"))
            {
                if (currentUser?.DepartmentId == null || post.DepartmentId != currentUser.DepartmentId)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền từ chối bài viết này!";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            await _postService.RejectAsync(id, currentUserId, feedback ?? "");
            InvalidateDashboardCache();
            TempData["SuccessMessage"] = $"❌ Đã từ chối bài viết '{post.Title}'.";

            // Email notify author
            try
            {
                if (!string.IsNullOrEmpty(post.AuthorId))
                {
                    var author = await _userManager.FindByIdAsync(post.AuthorId);
                    if (author != null && !string.IsNullOrEmpty(author.Email))
                    {
                        var postUrl = Url.Action("Details", "Posts", new { id = post.Id }, Request.Scheme) ?? "";
                        var reviewerName = currentUser?.FullName ?? currentUser?.UserName ?? "QA Coordinator";
                        var emailBody = $@"<p>Xin chào <strong>{author.FullName ?? author.UserName}</strong>,</p>
<p>Rất tiếc, bài viết <strong>'{post.Title}'</strong> của bạn đã bị từ chối bởi <strong>{reviewerName}</strong>.</p>
<p><strong>Lý do từ chối:</strong></p><blockquote style='background:#f8f8f8;border-left:4px solid #e53e3e;padding:12px;'>{System.Web.HttpUtility.HtmlEncode(feedback ?? "Không có lý do cụ thể.")}</blockquote>
<p>Bạn có thể <a href='{postUrl}'>xem bài viết tại đây</a>.</p>";
                        await _emailSender.SendEmailAsync(author.Email, $"[CMS Portal] Bài viết '{post.Title}' bị từ chối", emailBody);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gửi email thông báo từ chối bài {PostId}", id);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Posts/RequestChanges/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,QA Coordinator")]
        public async Task<IActionResult> RequestChanges(Guid id, string feedback)
        {
            var post = await _postService.GetByIdAsync(id);
            if (post == null) return NotFound();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var currentUser = !string.IsNullOrEmpty(currentUserId) ? await _userManager.FindByIdAsync(currentUserId) : null;

            if (User.IsInRole("QA Coordinator") && !User.IsInRole("Admin"))
            {
                if (currentUser?.DepartmentId == null || post.DepartmentId != currentUser.DepartmentId)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền yêu cầu sửa đổi bài viết này!";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            await _postService.RequestChangesAsync(id, currentUserId, feedback ?? "");
            InvalidateDashboardCache();
            TempData["SuccessMessage"] = $"🔄 Đã gửi yêu cầu sửa đổi tới tác giả bài viết '{post.Title}'.";

            // Email notify author
            try
            {
                if (!string.IsNullOrEmpty(post.AuthorId))
                {
                    var author = await _userManager.FindByIdAsync(post.AuthorId);
                    if (author != null && !string.IsNullOrEmpty(author.Email))
                    {
                        var postUrl = Url.Action("Edit", "Posts", new { id = post.Id }, Request.Scheme) ?? "";
                        var reviewerName = currentUser?.FullName ?? currentUser?.UserName ?? "QA Coordinator";
                        var emailBody = $@"<p>Xin chào <strong>{author.FullName ?? author.UserName}</strong>,</p>
<p>Bài viết <strong>'{post.Title}'</strong> của bạn cần được sửa đổi trước khi duyệt. Đây là phản hồi từ <strong>{reviewerName}</strong>:</p>
<blockquote style='background:#fffbeb;border-left:4px solid #f6ad55;padding:12px;'>{System.Web.HttpUtility.HtmlEncode(feedback ?? "Vui lòng xem lại nội dung bài viết.")}</blockquote>
<p>Bạn có thể <a href='{postUrl}'>chỉnh sửa bài viết tại đây</a>.</p>";
                        await _emailSender.SendEmailAsync(author.Email, $"[CMS Portal] Bài viết '{post.Title}' cần sửa đổi", emailBody);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gửi email yêu cầu sửa đổi bài {PostId}", id);
            }

            return RedirectToAction(nameof(Details), new { id });
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
            InvalidateDashboardCache();
            return RedirectToAction(nameof(Index));
        }
    }
}

