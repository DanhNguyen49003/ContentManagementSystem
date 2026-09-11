using ContentManagementSystem.DataLayer;
using ContentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ContentManagementSystem.ViewComponents
{
    public class HeaderViewComponent : ViewComponent
    {
        private readonly ContentManageDbContext _context;

        public HeaderViewComponent(ContentManageDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var menu = await _context.Menus
                .FirstOrDefaultAsync(m => m.Position == "Header");

            var menuItems = new System.Collections.Generic.List<ContentManagementSystem.ApplicationCore.Entities.MenuItem>();

            if (menu != null)
            {
                menuItems = await _context.MenuItems
                    .Where(m => m.MenuId == menu.Id)
                    .ToListAsync();
            }

            var vm = new HeaderViewModel
            {
                MainMenu = menu!,
                MenuItems = menuItems
            };

            return View(vm);
        }
    }
}
