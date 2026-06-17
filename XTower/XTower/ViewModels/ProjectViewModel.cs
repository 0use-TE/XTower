using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Crystal.Avalonia;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using XTower.Models.Content;
using XTower.Services;

namespace XTower.ViewModels
{
    internal partial class ProjectViewModel : DockViewModel, ILifecycleAware
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly IContentStore _contentStore;
        private readonly ILevelEditorSession _editorSession;
        private readonly IDockNavigationService _dockNavigation;
        private readonly IEditorStateService _editorState;
        private readonly IEditorCommandService _editorCommands;

        public override string Id => "project";

        public override string Header => "项目";

        public override DockPosition DockPosition => DockPosition.Left;

        public override bool IsClosable => false;

        [ObservableProperty]
        private string _workspacePath = "未打开工作区";

        [ObservableProperty]
        private string _newLevelId = string.Empty;

        [ObservableProperty]
        private LevelItemViewModel? _selectedLevel;

        [ObservableProperty]
        private bool _hasSelectedLevel;

        [ObservableProperty]
        private string _editorStatus = "未选择关卡";

        [ObservableProperty]
        private int _levelColumns = 32;

        [ObservableProperty]
        private int _levelRows = 18;

        [ObservableProperty]
        private int _levelCellSize = 32;

        [ObservableProperty]
        private string _levelBackground = "无";

        public ObservableCollection<LevelItemViewModel> Levels { get; } = [];

        private bool _loadingLevelDetail;

        private bool _restoredLastSession;

        public ProjectViewModel(
            IWorkspaceService workspaceService,
            IContentStore contentStore,
            ILevelEditorSession editorSession,
            IDockNavigationService dockNavigation,
            IEditorStateService editorState,
            IEditorCommandService editorCommands)
        {
            _workspaceService = workspaceService;
            _contentStore = contentStore;
            _editorSession = editorSession;
            _dockNavigation = dockNavigation;
            _editorState = editorState;
            _editorCommands = editorCommands;

            _workspaceService.WorkspaceChanged += (_, _) =>
            {
                _restoredLastSession = false;
                RefreshAll();
            };
            _editorSession.LevelChanged += (_, _) => LoadLevelDetailFromSession();
            _editorSession.LevelSettingsChanged += (_, _) => LoadLevelDetailFromSession();
            _editorCommands.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IEditorCommandService.StatusText))
                    EditorStatus = _editorCommands.StatusText;
            };
        }

        partial void OnSelectedLevelChanged(LevelItemViewModel? value)
        {
            if (value is not null && _workspaceService.IsOpen)
            {
                _editorSession.LoadLevel(value.Id);
                _dockNavigation.ShowTab<LevelViewModel>();
                _dockNavigation.ShowTab<LevelPathsViewModel>();
                LoadLevelDetailFromSession();
                return;
            }

            HasSelectedLevel = false;
            EditorStatus = "未选择关卡";
        }

        partial void OnLevelColumnsChanged(int value)
        {
            if (!_loadingLevelDetail)
                SyncLevelGrid();
        }

        partial void OnLevelRowsChanged(int value)
        {
            if (!_loadingLevelDetail)
                SyncLevelGrid();
        }

        partial void OnLevelCellSizeChanged(int value)
        {
            if (!_loadingLevelDetail)
                SyncLevelGrid();
        }

        [RelayCommand]
        private async Task OpenWorkspaceAsync()
        {
            var path = await StorageDialogService.PickFolderAsync("选择工作区根目录");
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!_workspaceService.TryOpen(path))
                WorkspacePath = "打开工作区失败";
        }

        [RelayCommand]
        private void CreateLevel()
        {
            if (!_workspaceService.IsOpen || string.IsNullOrWhiteSpace(NewLevelId))
                return;

            var levelId = NewLevelId.Trim();
            if (_contentStore.ListLevelIds().Any(id => string.Equals(id, levelId, StringComparison.OrdinalIgnoreCase)))
                return;

            _contentStore.CreateLevel(levelId);
            NewLevelId = string.Empty;
            RefreshLevels();

            SelectedLevel = Levels.FirstOrDefault(level => string.Equals(level.Id, levelId, StringComparison.Ordinal));
        }

        [RelayCommand]
        private void DeleteLevel()
        {
            if (!_workspaceService.IsOpen || SelectedLevel is null)
                return;

            _contentStore.DeleteLevel(SelectedLevel.Id);
            SelectedLevel = null;
            RefreshLevels();
        }

        [RelayCommand]
        private async Task ImportBackgroundAsync()
        {
            var level = _editorSession.CurrentLevel;
            if (level is null)
                return;

            var sourcePath = await StorageDialogService.PickImageAsync("导入关卡背景图");
            if (string.IsNullOrWhiteSpace(sourcePath))
                return;

            var relativePath = _contentStore.ImportLevelBackground(level.Id, sourcePath);
            level.Background = new BackgroundConfig
            {
                Image = relativePath,
                Stretch = "Fill",
            };

            LevelBackground = relativePath;
            _editorSession.NotifyLevelSettingsChanged();
        }

        [RelayCommand]
        private void Refresh() => RefreshAll();

        public Task OnLoadedAsync()
        {
            RefreshAll();
            return Task.CompletedTask;
        }

        public Task OnUnloaded() => Task.CompletedTask;

        private void RefreshAll()
        {
            WorkspacePath = _workspaceService.IsOpen ? _workspaceService.RootPath ?? "未打开工作区" : "未打开工作区";

            if (!_workspaceService.IsOpen)
            {
                Levels.Clear();
                HasSelectedLevel = false;
                EditorStatus = "未选择关卡";
                return;
            }

            _editorSession.RefreshProject();
            RefreshLevels();
            TryRestoreLastSession();
            LoadLevelDetailFromSession();
        }

        private void TryRestoreLastSession()
        {
            if (_restoredLastSession || SelectedLevel is not null)
                return;

            var levelId = _editorState.LastLevelId;
            if (string.IsNullOrWhiteSpace(levelId))
                return;

            var level = Levels.FirstOrDefault(item => string.Equals(item.Id, levelId, StringComparison.Ordinal));
            if (level is null)
                return;

            _restoredLastSession = true;
            SelectedLevel = level;
        }

        private void RefreshLevels()
        {
            var selectedLevelId = SelectedLevel?.Id;
            Levels.Clear();

            foreach (var levelId in _contentStore.ListLevelIds())
            {
                var level = _contentStore.LoadLevel(levelId);
                Levels.Add(new LevelItemViewModel(levelId, level?.Name ?? levelId));
            }

            if (!string.IsNullOrWhiteSpace(selectedLevelId))
                SelectedLevel = Levels.FirstOrDefault(level => string.Equals(level.Id, selectedLevelId, StringComparison.Ordinal));
        }

        private void LoadLevelDetailFromSession()
        {
            var level = _editorSession.CurrentLevel;
            if (level is null || SelectedLevel is null)
            {
                HasSelectedLevel = false;
                if (SelectedLevel is null)
                    EditorStatus = "未选择关卡";
                return;
            }

            _loadingLevelDetail = true;
            try
            {
                HasSelectedLevel = true;
                EditorStatus = _editorCommands.StatusText;
                LevelColumns = level.Grid.Columns;
                LevelRows = level.Grid.Rows;
                LevelCellSize = level.Grid.CellSize;
                LevelBackground = string.IsNullOrWhiteSpace(level.Background?.Image) ? "无" : level.Background!.Image;
            }
            finally
            {
                _loadingLevelDetail = false;
            }
        }

        private void SyncLevelGrid()
        {
            var level = _editorSession.CurrentLevel;
            if (level is null)
                return;

            level.Grid.Columns = LevelColumns;
            level.Grid.Rows = LevelRows;
            level.Grid.CellSize = LevelCellSize;
            _editorSession.NotifyLevelSettingsChanged();
        }
    }

    internal sealed class LevelItemViewModel
    {
        public LevelItemViewModel(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; }

        public string Name { get; }

        public string Title => $"{Name} ({Id})";
    }
}
