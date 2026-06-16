using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections;
using System.Collections.Generic;

namespace XTower.ViewModels
{
    internal partial class MainViewModel : ViewModelBase
    {
        public MainViewModel(IEnumerable<DockViewModel> dockViewModels)
        {
        }

    }
}
