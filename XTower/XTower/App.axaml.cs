using Avalonia;
using Avalonia.Markup.Xaml;
using Crystal.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using System;
using XTower.ViewModels;
using XTower.Views;

namespace XTower
{
    public partial class App : CrystalApplication
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);
        public override void RegisterServices(IServiceCollection services)
        {
            //日志
            Logger logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.Debug()
                .CreateLogger();
            services.AddLogging(options=>options.AddSerilog(logger));
            services.AddMvvmSingleton<MainView, MainViewModel>();
            services.AddDockTabItem<ProjectView, ProjectViewModel>();
            services.AddDockTabItem<MonsterView, MonsterViewModel>();
            services.AddDockTabItem<TurretView, TurretViewModel>();
            services.AddDockTabItem<MusicView, MusicViewModel>();
        }

        public override void CreateShell(IServiceProvider serviceProvider)
        {
            CreateShell<MainWindow, MainView>();
        }
    }
}
