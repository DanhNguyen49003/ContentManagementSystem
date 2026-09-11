using ContentManagementSystem.ApplicationCore.Entities;
using System.Collections.Generic;

namespace ContentManagementSystem.ViewModels
{
    public class SidebarViewModel
    {
        public List<Category> Categories { get; set; } = new();
        public List<Post> RecentPosts { get; set; } = new();
    }
}
