using ContentManagementSystem.ApplicationCore.Entities;
using System.Collections.Generic;

namespace ContentManagementSystem.ViewModels
{
    public class HeaderViewModel
    {
        public Menu? MainMenu { get; set; }
        public List<MenuItem> MenuItems { get; set; } = new();
    }
}
