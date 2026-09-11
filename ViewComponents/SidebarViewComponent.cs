using ContentManagementSystem.DataLayer;
using ContentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ContentManagementSystem.ViewComponents
{
    public class SidebarViewComponent : ViewComponent
    {
        private readonly ContentManageDbContext _context;

        public SidebarViewComponent(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            var recentPosts = await _context.Posts
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            var vm = new SidebarViewModel
            {
                Categories = categories,
                RecentPosts = recentPosts
            };

            return View(vm);
        }
    }
}
