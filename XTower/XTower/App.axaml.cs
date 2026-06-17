using Avalonia.Markup.Xaml;
using Crystal.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Compact;
using System;
using System.IO;
using XTower.Persistence;
using XTower.ViewModels;
using XTower.Views;

namespace XTower
{
    public partial class App : CrystalApplication
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void RegisterServices(IServiceCollection services)
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "XTower",
                "LocalData");

            Directory.CreateDirectory(logDirectory);

            Logger logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.Debug()
                .WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: Path.Combine(logDirectory, "log-.jsonl"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();

            services.AddLogging(options => options.AddSerilog(logger));
            services.AddSingleton<IDataPersistence, DataPersistence>();

            services.AddMvvmSingleton<MainView, MainViewModel>();
            services.AddDockTabItem<ProjectView, ProjectViewModel>();
            services.AddDockTabItem<MonsterView, MonsterViewModel>();
            services.AddDockTabItem<TurretView, TurretViewModel>();
            services.AddDockTabItem<MusicView, MusicViewModel>();
        }

        public override void CreateShell(IServiceProvider serviceProvider) =>
            CreateShell<MainWindow, MainView>();
    }
}
