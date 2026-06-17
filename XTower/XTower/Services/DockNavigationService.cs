using System;
using Microsoft.Extensions.DependencyInjection;
using XTower.ViewModels;

namespace XTower.Services
{
    internal sealed class DockNavigationService : IDockNavigationService
    {
        private readonly IServiceProvider _serviceProvider;

        public DockNavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void ShowTab<TTab>() where TTab : DockViewModel
        {
            var main = _serviceProvider.GetRequiredService<MainViewModel>();
            var tab = _serviceProvider.GetRequiredService<TTab>();
            main.ShowDockTab(tab);
        }
    }
}
