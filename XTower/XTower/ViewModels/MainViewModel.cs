using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Crystal.Avalonia;
using GOZA.Dock;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using XTower.Extensions;
using XTower.Persistence;
using XTower.Services;

namespace XTower.ViewModels
{
    internal partial class MainViewModel : ViewModelBase, ILifecycleAware
    {
        private const string LayoutStorageName = "layout.jsonl";

        public ObservableCollection<IDockTabItem> LeftItems { get; } = new();
        public ObservableCollection<IDockTabItem> CenterTopItems { get; } = new();
        public ObservableCollection<IDockTabItem> CenterBottomItems { get; } = new();
        public ObservableCollection<IDockTabItem> RightItems { get; } = new();

        [ObservableProperty]
        private IDockTabItem? _leftSelectedItem;

        [ObservableProperty]
        private IDockTabItem? _centerTopSelectedItem;

        [ObservableProperty]
        private IDockTabItem? _centerBottomSelectedItem;

        [ObservableProperty]
        private IDockTabItem? _rightSelectedItem;

        [ObservableProperty]
        private GridLength _leftGridLength = new(250);

        [ObservableProperty]
        private GridLength _rightGridLength = new(280);

        [ObservableProperty]
        private GridLength _centerColumnWidth = new(1, GridUnitType.Star);

        [ObservableProperty]
        private GridLength _leftSplitterColumnWidth = new(2);

        [ObservableProperty]
        private GridLength _rightSplitterColumnWidth = new(2);

        [ObservableProperty]
        private GridLength _centerTopGridLength = new(5, GridUnitType.Star);

        [ObservableProperty]
        private GridLength _centerBottomGridLength = new(3, GridUnitType.Star);

        [ObservableProperty]
        private GridLength _centerHSplitterRowHeight = new(2);

        [ObservableProperty]
        private bool _isDarkMode;

        public IEnumerable<IDockTabItem> Items =>
            LeftItems.Concat(CenterTopItems).Concat(CenterBottomItems).Concat(RightItems);

        private readonly IServiceProvider _serviceProvider;
        private readonly IDataPersistence _dataPersistence;
        private readonly IWorkspaceService _workspaceService;
        private readonly IReadOnlyDictionary<string, DockViewModel> _dockItemsById;
        private readonly IEditorCommandService _editorCommands;
        private readonly Dictionary<string, DockPosition> _preferredRegions = new(StringComparer.Ordinal);
        private bool _suspendAutoSave;
        private IDisposable? _layoutSubscription;

        public IEditorCommandService Editor => _editorCommands;

        public MainViewModel(
            IEnumerable<DockViewModel> dockItems,
            IServiceProvider serviceProvider,
            IDataPersistence dataPersistence,
            IWorkspaceService workspaceService,
            IEditorCommandService editorCommands)
        {
            _serviceProvider = serviceProvider;
            _dataPersistence = dataPersistence;
            _workspaceService = workspaceService;
            _editorCommands = editorCommands;
            _dockItemsById = dockItems.ToDictionary(x => x.Id);

            _suspendAutoSave = true;
            var storedLayout = _dataPersistence.Load(LayoutStorageName, MainViewJsonContext.Default.MainViewStorageModel)
                ?? new MainViewStorageModel();
            ApplyPersistedTabLayout(storedLayout);
            LoadPreferredRegions(storedLayout);

            SubscribeRegionLayout(LeftItems, DockPosition.Left);
            SubscribeRegionLayout(CenterTopItems, DockPosition.CenterTop);
            SubscribeRegionLayout(CenterBottomItems, DockPosition.CenterBottom);
            SubscribeRegionLayout(RightItems, DockPosition.Right);
            _suspendAutoSave = false;
        }

        public Task OnLoadedAsync()
        {
            if (_layoutSubscription is not null)
                return Task.CompletedTask;

            _suspendAutoSave = true;

            _workspaceService.EnsureInitialized();

            var stored = _dataPersistence.Load(LayoutStorageName, MainViewJsonContext.Default.MainViewStorageModel)
                ?? new MainViewStorageModel();

            LeftGridLength = new GridLength(stored.Left);
            RightGridLength = new GridLength(stored.Right);
            CenterTopGridLength = new GridLength(stored.CenterTopBottomProportion, GridUnitType.Star);
            CenterBottomGridLength = new GridLength(1, GridUnitType.Star);
            IsDarkMode = stored.IsDarkMode;

            if (Application.Current is not null)
                Application.Current.RequestedThemeVariant = IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;

            RestoreSelectedTabs(stored);
            EnsurePinnedProjectPanel();

            _suspendAutoSave = false;

            _layoutSubscription = this.ObserveProperty(
                    nameof(LeftGridLength),
                    nameof(RightGridLength),
                    nameof(CenterTopGridLength),
                    nameof(CenterBottomGridLength),
                    nameof(CenterColumnWidth),
                    nameof(LeftSplitterColumnWidth),
                    nameof(RightSplitterColumnWidth),
                    nameof(CenterHSplitterRowHeight))
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(_ => Save());

            return Task.CompletedTask;
        }

        public Task OnUnloaded()
        {
            Save();
            return Task.CompletedTask;
        }

        partial void OnLeftSelectedItemChanged(IDockTabItem? value) => Save();

        partial void OnCenterTopSelectedItemChanged(IDockTabItem? value) => Save();

        partial void OnCenterBottomSelectedItemChanged(IDockTabItem? value) => Save();

        partial void OnRightSelectedItemChanged(IDockTabItem? value) => Save();

        [RelayCommand]
        private void SwitchTheme()
        {
            if (Application.Current is null)
                return;

            if (Application.Current.ActualThemeVariant == ThemeVariant.Dark)
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                IsDarkMode = false;
            }
            else
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
                IsDarkMode = true;
            }

            Save();
        }

        [RelayCommand]
        private void ToggleMonsterView() => Toggle(_serviceProvider.GetRequiredService<MonsterViewModel>());

        [RelayCommand]
        private void ToggleTurretView() => Toggle(_serviceProvider.GetRequiredService<TurretViewModel>());

        [RelayCommand]
        private void ToggleMusicView() => Toggle(_serviceProvider.GetRequiredService<MusicViewModel>());

        [RelayCommand]
        private void ToggleLevelView() => Toggle(_serviceProvider.GetRequiredService<LevelViewModel>());

        [RelayCommand]
        private void ToggleLevelPathsView() => Toggle(_serviceProvider.GetRequiredService<LevelPathsViewModel>());

        private void Toggle(DockViewModel item)
        {
            if (!item.IsClosable)
            {
                ShowDockTab(item);
                return;
            }

            if (!Items.Contains(item))
            {
                ShowDockTab(item);
                return;
            }

            if (IsSelected(item))
                Close(item);
            else
                SelectItem(item);
        }

        private void EnsurePinnedProjectPanel()
        {
            var project = _serviceProvider.GetRequiredService<ProjectViewModel>();
            if (!LeftItems.Contains(project))
            {
                _preferredRegions[project.Id] = DockPosition.Left;
                LeftItems.Insert(0, project);
            }

            LeftSelectedItem ??= project;
        }

        internal void ShowDockTab(DockViewModel item)
        {
            if (!Items.Contains(item))
                AddToRegion(item, GetPreferredRegion(item));

            SelectItem(item);
        }

        private void SelectItem(DockViewModel item)
        {
            if (LeftItems.Contains(item))
                LeftSelectedItem = item;
            else if (CenterTopItems.Contains(item))
                CenterTopSelectedItem = item;
            else if (CenterBottomItems.Contains(item))
                CenterBottomSelectedItem = item;
            else if (RightItems.Contains(item))
                RightSelectedItem = item;
        }

        private void Close(DockViewModel item)
        {
            if (!item.IsClosable)
                return;

            if (LeftItems.Contains(item))
                LeftItems.Remove(item);
            else if (CenterTopItems.Contains(item))
                CenterTopItems.Remove(item);
            else if (CenterBottomItems.Contains(item))
                CenterBottomItems.Remove(item);
            else if (RightItems.Contains(item))
                RightItems.Remove(item);
        }

        private bool IsSelected(DockViewModel item) =>
            item switch
            {
                _ when LeftItems.Contains(item) => LeftSelectedItem == item,
                _ when CenterTopItems.Contains(item) => CenterTopSelectedItem == item,
                _ when CenterBottomItems.Contains(item) => CenterBottomSelectedItem == item,
                _ when RightItems.Contains(item) => RightSelectedItem == item,
                _ => false
            };

        private void Save()
        {
            if (_suspendAutoSave)
                return;

            var model = new MainViewStorageModel
            {
                Left = LeftGridLength.Value,
                Right = RightGridLength.Value,
                CenterTopBottomProportion = CenterBottomGridLength.Value > 0
                    ? CenterTopGridLength.Value / CenterBottomGridLength.Value
                    : 1.5,
                LeftItems = LeftItems.OfType<DockViewModel>().Select(x => x.Id).ToList(),
                RightItems = RightItems.OfType<DockViewModel>().Select(x => x.Id).ToList(),
                CenterTopItems = CenterTopItems.OfType<DockViewModel>().Select(x => x.Id).ToList(),
                CenterBottomItems = CenterBottomItems.OfType<DockViewModel>().Select(x => x.Id).ToList(),
                LeftSelectedTabId = (LeftSelectedItem as DockViewModel)?.Id,
                CenterTopSelectedTabId = (CenterTopSelectedItem as DockViewModel)?.Id,
                CenterBottomSelectedTabId = (CenterBottomSelectedItem as DockViewModel)?.Id,
                RightSelectedTabId = (RightSelectedItem as DockViewModel)?.Id,
                IsDarkMode = IsDarkMode,
                TabPreferredRegions = _preferredRegions.ToDictionary(
                    static pair => pair.Key,
                    static pair => (int)pair.Value),
            };

            _dataPersistence.Save(LayoutStorageName, model, MainViewJsonContext.Default.MainViewStorageModel);
        }

        private void OnTabLayoutChanged(DockPosition region, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
            {
                foreach (IDockTabItem item in e.NewItems)
                {
                    if (item is DockViewModel vm)
                        _preferredRegions[vm.Id] = region;
                }
            }

            Save();
        }

        private void SubscribeRegionLayout(ObservableCollection<IDockTabItem> items, DockPosition region) =>
            items.CollectionChanged += (_, e) => OnTabLayoutChanged(region, e);

        private void ApplyPersistedTabLayout(MainViewStorageModel model)
        {
            var lookup = BuildTabSourceLookup();
            var assignedIds = new HashSet<string>(StringComparer.Ordinal);

            RebuildContainer(LeftItems, model.LeftItems, lookup, assignedIds);
            RebuildContainer(RightItems, model.RightItems, lookup, assignedIds);
            RebuildContainer(CenterTopItems, model.CenterTopItems, lookup, assignedIds);
            RebuildContainer(CenterBottomItems, model.CenterBottomItems, lookup, assignedIds);

            foreach (var item in lookup.Values.Where(item => !assignedIds.Contains(item.Id)))
                AddToRegion(item, GetPreferredRegion(item));

            SyncPreferredRegionsFromCollections();
        }

        private void LoadPreferredRegions(MainViewStorageModel model)
        {
            _preferredRegions.Clear();

            foreach (var (id, regionValue) in model.TabPreferredRegions)
            {
                if (Enum.IsDefined(typeof(DockPosition), regionValue))
                    _preferredRegions[id] = (DockPosition)regionValue;
            }
        }

        private void SyncPreferredRegionsFromCollections()
        {
            RecordPreferredRegion(LeftItems, DockPosition.Left);
            RecordPreferredRegion(CenterTopItems, DockPosition.CenterTop);
            RecordPreferredRegion(CenterBottomItems, DockPosition.CenterBottom);
            RecordPreferredRegion(RightItems, DockPosition.Right);
        }

        private void RecordPreferredRegion(ObservableCollection<IDockTabItem> items, DockPosition region)
        {
            foreach (var item in items.OfType<DockViewModel>())
                _preferredRegions[item.Id] = region;
        }

        private DockPosition GetPreferredRegion(DockViewModel item) =>
            _preferredRegions.TryGetValue(item.Id, out var region) ? region : item.DockPosition;

        private Dictionary<string, DockViewModel> BuildTabSourceLookup() =>
            _dockItemsById.Values
                .GroupBy(static item => item.Id, StringComparer.Ordinal)
                .Select(static g => g.First())
                .ToDictionary(static item => item.Id, static item => item, StringComparer.Ordinal);

        private static void RebuildContainer(
            ObservableCollection<IDockTabItem> target,
            IEnumerable<string>? persistedIds,
            IReadOnlyDictionary<string, DockViewModel> sourceLookup,
            ISet<string> assignedIds)
        {
            target.Clear();

            if (persistedIds is null)
                return;

            foreach (var id in persistedIds)
            {
                if (string.IsNullOrWhiteSpace(id) || assignedIds.Contains(id))
                    continue;

                if (sourceLookup.TryGetValue(id, out var item))
                {
                    target.Add(item);
                    assignedIds.Add(id);
                }
            }
        }

        private void RestoreSelectedTabs(MainViewStorageModel model)
        {
            LeftSelectedItem = ResolveTab(model.LeftSelectedTabId, LeftItems) ?? LeftItems.FirstOrDefault();
            CenterTopSelectedItem = ResolveTab(model.CenterTopSelectedTabId, CenterTopItems) ?? CenterTopItems.FirstOrDefault();
            CenterBottomSelectedItem = ResolveTab(model.CenterBottomSelectedTabId, CenterBottomItems) ?? CenterBottomItems.FirstOrDefault();
            RightSelectedItem = ResolveTab(model.RightSelectedTabId, RightItems) ?? RightItems.FirstOrDefault();
        }

        private static IDockTabItem? ResolveTab(string? id, IEnumerable<IDockTabItem> items)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return items.FirstOrDefault(item => item is DockViewModel vm && vm.Id == id);
        }

        private void AddToRegion(DockViewModel item, DockPosition region)
        {
            _preferredRegions[item.Id] = region;

            switch (region)
            {
                case DockPosition.Left:
                    LeftItems.Add(item);
                    break;
                case DockPosition.CenterTop:
                    CenterTopItems.Add(item);
                    break;
                case DockPosition.CenterBottom:
                    CenterBottomItems.Add(item);
                    break;
                case DockPosition.Right:
                    RightItems.Add(item);
                    break;
            }
        }

        [JsonSerializable(typeof(MainViewStorageModel))]
        [JsonSerializable(typeof(Dictionary<string, int>))]
        [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
        internal partial class MainViewJsonContext : JsonSerializerContext;

        internal sealed class MainViewStorageModel
        {
            public double Left { get; set; } = 250;
            public double Right { get; set; } = 280;
            public double CenterTopBottomProportion { get; set; } = 5d / 3d;
            public bool IsDarkMode { get; set; }
            public Dictionary<string, int> TabPreferredRegions { get; set; } = new(StringComparer.Ordinal);
            public List<string> LeftItems { get; set; } = [];
            public List<string> RightItems { get; set; } = [];
            public List<string> CenterTopItems { get; set; } = [];
            public List<string> CenterBottomItems { get; set; } = [];
            public string? LeftSelectedTabId { get; set; }
            public string? CenterTopSelectedTabId { get; set; }
            public string? CenterBottomSelectedTabId { get; set; }
            public string? RightSelectedTabId { get; set; }
        }
    }
}
