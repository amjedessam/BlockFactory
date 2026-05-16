using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface INavigationService
    {
        void NavigateTo(string viewName);
        void NavigateBack();
        string CurrentView { get; }
        event Action<string> OnNavigated;
    }
}
