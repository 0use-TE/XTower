using Avalonia.Controls;
using Crystal.Avalonia;
using GOZA.Dock;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace XTower.ViewModels
{
    internal enum DockPosition
    {
        Left,
        Right,
        CenterTop,
        CenterBottom
    }
    internal static partial class  Extensions
    {
        public static void AddDockTabItem<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]View, 
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] ViewModel>
            (this IServiceCollection services) where View : Control where ViewModel : DockViewModel
        {
            services.AddMvvmSingleton<View, ViewModel>();
            services.AddSingleton<DockViewModel>(sp=>sp.GetRequiredService<ViewModel>());
        }
    }
    internal abstract class DockViewModel : ViewModelBase, IDockTabItem
    {
        public abstract string Id { get; }
        public abstract string Header { get; }
        public abstract DockPosition DockPosition { get; }
        public virtual bool ReuseSurface => false;
        public virtual bool IsClosable => true;
    }
}
