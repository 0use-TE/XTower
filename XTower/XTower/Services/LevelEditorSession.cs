using System;
using XTower.Models.Content;

namespace XTower.Services
{
    internal sealed class LevelEditorSession : ILevelEditorSession
    {
        private readonly IContentStore _contentStore;
        private readonly IWorkspaceService _workspaceService;
        private readonly IEditorStateService _editorState;
        private string? _selectedPathId;

        public LevelEditorSession(
            IContentStore contentStore,
            IWorkspaceService workspaceService,
            IEditorStateService editorState)
        {
            _contentStore = contentStore;
            _workspaceService = workspaceService;
            _editorState = editorState;

            if (_workspaceService.IsOpen)
                RefreshProject();
        }

        public LevelDefinition? CurrentLevel { get; private set; }

        public ProjectConfig? Project { get; private set; }

        public PathDefinition? SelectedPath =>
            CurrentLevel?.Paths.Find(path => string.Equals(path.Id, _selectedPathId, StringComparison.Ordinal));

        public string? SelectedPathId => _selectedPathId;

        public event EventHandler? LevelChanged;

        public event EventHandler? ProjectChanged;

        public event EventHandler? SelectedPathChanged;

        public event EventHandler? PathsChanged;

        public event EventHandler? LevelSettingsChanged;

        public void RefreshProject()
        {
            Project = _workspaceService.IsOpen
                ? _contentStore.LoadProject()
                : new ProjectConfig();
            ProjectChanged?.Invoke(this, EventArgs.Empty);
        }

        public void LoadLevel(string levelId)
        {
            CurrentLevel = _contentStore.LoadLevel(levelId);
            if (CurrentLevel is not null)
            {
                PathDefaults.EnsurePaths(CurrentLevel);
                _editorState.RememberLevel(levelId);
                _selectedPathId = ResolvePathId(_editorState.LastPathId) ?? CurrentLevel.Paths[0].Id;
            }
            else
            {
                _selectedPathId = null;
            }

            LevelChanged?.Invoke(this, EventArgs.Empty);
            SelectedPathChanged?.Invoke(this, EventArgs.Empty);
            PathsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SaveCurrentLevel()
        {
            if (CurrentLevel == null)
                return;

            _contentStore.SaveLevel(CurrentLevel);
        }

        public void SetProject(ProjectConfig project)
        {
            _contentStore.SaveProject(project);
            Project = project;
            ProjectChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SelectPath(string pathId)
        {
            if (CurrentLevel is null)
                return;

            if (CurrentLevel.Paths.Exists(path => string.Equals(path.Id, pathId, StringComparison.Ordinal)))
            {
                _selectedPathId = pathId;
                _editorState.RememberPath(pathId);
                SelectedPathChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public PathDefinition AddPath(string? name = null)
        {
            if (CurrentLevel is null)
                throw new InvalidOperationException("未选择关卡。");

            var id = PathDefaults.NewPathId(CurrentLevel);
            var path = PathDefaults.Create(id, name ?? $"路径 {CurrentLevel.Paths.Count + 1}", CurrentLevel.Paths.Count);
            CurrentLevel.Paths.Add(path);
            _selectedPathId = id;
            _editorState.RememberPath(id);
            PathsChanged?.Invoke(this, EventArgs.Empty);
            SelectedPathChanged?.Invoke(this, EventArgs.Empty);
            return path;
        }

        public void DeletePath(string pathId)
        {
            if (CurrentLevel is null)
                return;

            if (CurrentLevel.Paths.Count <= 1)
                return;

            CurrentLevel.Paths.RemoveAll(path => string.Equals(path.Id, pathId, StringComparison.Ordinal));

            if (string.Equals(_selectedPathId, pathId, StringComparison.Ordinal))
            {
                _selectedPathId = CurrentLevel.Paths[0].Id;
                _editorState.RememberPath(_selectedPathId);
            }

            PathsChanged?.Invoke(this, EventArgs.Empty);
            SelectedPathChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyPathsChanged() => PathsChanged?.Invoke(this, EventArgs.Empty);

        public void NotifyLevelSettingsChanged() => LevelSettingsChanged?.Invoke(this, EventArgs.Empty);

        private string? ResolvePathId(string? pathId)
        {
            if (CurrentLevel is null || string.IsNullOrWhiteSpace(pathId))
                return null;

            return CurrentLevel.Paths.Exists(path => string.Equals(path.Id, pathId, StringComparison.Ordinal))
                ? pathId
                : null;
        }
    }
}
