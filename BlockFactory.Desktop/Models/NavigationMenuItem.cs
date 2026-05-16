using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Input;

namespace BlockFactory.Desktop.Models
{
    public class NavigationMenuItem
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ViewName { get; set; } = string.Empty;
        public string? Permission { get; set; }
        public bool IsSelected { get; set; }
        public bool HasSeparatorAbove { get; set; }
        public ICommand? Command { get; set; }
    }

    public class NavigationMenuGroup
    {
        public string GroupTitle { get; set; } = string.Empty;
        public List<NavigationMenuItem> Items { get; set; } = new();
    }
}
