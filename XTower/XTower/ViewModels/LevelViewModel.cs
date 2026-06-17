using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using XTower.Controls;
using XTower.Models.Content;
using XTower.Services;

namespace XTower.ViewModels
{
    internal partial class LevelViewModel : DockViewModel
    {
        private readonly ILevelEditorSession _editorSession;
        private readonly IContentStore _contentStore;
        private readonly UndoStack<List<GridPoint>> _pathUndo = new();

        private bool _syncingWaypoints;

        public override string Id => "level";

        public override string Header => "关卡";

        public override DockPosition DockPosition => DockPosition.CenterTop;

        [ObservableProperty]
        private bool _hasLevel;

        [ObservableProperty]
        private string _levelTitle = "未选择关卡";

        [ObservableProperty]
        private string _activePathName = string.Empty;

        [ObservableProperty]
        private int _columns = 32;

        [ObservableProperty]
        private int _rows = 18;

        [ObservableProperty]
        private int _cellSize = 32;

        [ObservableProperty]
        private bool _isPathMode = true;

        [ObservableProperty]
        private bool _canUndo;

        [ObservableProperty]
        private bool _canRedo;

        [ObservableProperty]
        private Bitmap? _backgroundImage;

        public double CanvasWidth => Columns * CellSize + LevelCanvasControl.AxisLabelMargin * 2;

        public double CanvasHeight => Rows * CellSize + LevelCanvasControl.AxisLabelMargin * 2;

        public ObservableCollection<GridPoint> Waypoints { get; } = [];

        public ObservableCollection<PathRenderItem> PathLayers { get; } = [];

        public LevelViewModel(ILevelEditorSession editorSession, IContentStore contentStore)
        {
            _editorSession = editorSession;
            _contentStore = contentStore;
            _editorSession.LevelChanged += (_, _) => LoadFromSession();
            _editorSession.LevelSettingsChanged += (_, _) => LoadGridAndBackground();
            _editorSession.SelectedPathChanged += (_, _) => LoadSelectedPathWaypoints();
            _editorSession.PathsChanged += (_, _) =>
            {
                RefreshPathLayers();
                if (!_syncingWaypoints)
                    LoadSelectedPathWaypoints();
            };
            Waypoints.CollectionChanged += (_, _) => UpdateUndoState();
            LoadFromSession();
        }

        [RelayCommand]
        private void SaveLevel()
        {
            SyncWaypointsToLevel();
            _editorSession.SaveCurrentLevel();
        }

        [RelayCommand]
        private void AddPath()
        {
            if (!HasLevel)
                return;

            _editorSession.AddPath();
        }

        [RelayCommand]
        private void DeletePath()
        {
            if (!HasLevel || _editorSession.CurrentLevel!.Paths.Count <= 1)
                return;

            var pathId = _editorSession.SelectedPathId;
            if (string.IsNullOrWhiteSpace(pathId))
                return;

            _editorSession.DeletePath(pathId);
        }

        [RelayCommand]
        private void ClearPath()
        {
            if (Waypoints.Count == 0)
                return;

            PushUndoSnapshot();
            Waypoints.Clear();
            SyncWaypointsToLevel();
            UpdateUndoState();
        }

        [RelayCommand]
        private void UndoPath()
        {
            var snapshot = _pathUndo.Undo(CloneWaypoints());
            if (snapshot is null)
                return;

            ApplyWaypoints(snapshot);
            UpdateUndoState();
        }

        [RelayCommand]
        private void RedoPath()
        {
            var snapshot = _pathUndo.Redo(CloneWaypoints());
            if (snapshot is null)
                return;

            ApplyWaypoints(snapshot);
            UpdateUndoState();
        }

        [RelayCommand]
        private void HandleCellClick(GridPoint point)
        {
            if (!IsPathMode || !HasLevel || _editorSession.SelectedPath is null)
                return;

            PushUndoSnapshot();
            Waypoints.Add(new GridPoint { Col = point.Col, Row = point.Row });
            SyncWaypointsToLevel();
            UpdateUndoState();
        }

        private void LoadFromSession()
        {
            var level = _editorSession.CurrentLevel;
            HasLevel = level is not null;
            _pathUndo.Clear();
            UpdateUndoState();

            if (level is null)
            {
                LevelTitle = "未选择关卡";
                ActivePathName = string.Empty;
                Waypoints.Clear();
                PathLayers.Clear();
                BackgroundImage?.Dispose();
                BackgroundImage = null;
                return;
            }

            LevelTitle = $"{level.Name} ({level.Id})";
            LoadGridAndBackground();
            LoadSelectedPathWaypoints();
            RefreshPathLayers();
        }

        private void LoadGridAndBackground()
        {
            var level = _editorSession.CurrentLevel;
            if (level is null)
                return;

            Columns = level.Grid.Columns;
            Rows = level.Grid.Rows;
            CellSize = level.Grid.CellSize;
            OnPropertyChanged(nameof(CanvasWidth));
            OnPropertyChanged(nameof(CanvasHeight));
            LoadBackgroundImage(level);
        }

        private void LoadSelectedPathWaypoints()
        {
            var path = _editorSession.SelectedPath;
            ActivePathName = path?.Name ?? string.Empty;
            _pathUndo.Clear();
            UpdateUndoState();

            Waypoints.Clear();
            if (path is null)
                return;

            foreach (var waypoint in path.Waypoints)
                Waypoints.Add(new GridPoint { Col = waypoint.Col, Row = waypoint.Row });
        }

        private void RefreshPathLayers()
        {
            PathLayers.Clear();
            var level = _editorSession.CurrentLevel;
            if (level is null)
                return;

            foreach (var path in level.Paths)
            {
                PathLayers.Add(new PathRenderItem
                {
                    Waypoints = path.Waypoints,
                    Color = PathColorHelper.ToColor(path.Color, Colors.OrangeRed),
                    IsSelected = string.Equals(path.Id, _editorSession.SelectedPathId, StringComparison.Ordinal),
                });
            }
        }

        private void LoadBackgroundImage(LevelDefinition level)
        {
            BackgroundImage?.Dispose();
            BackgroundImage = null;

            var path = _contentStore.ResolveBackgroundAbsolutePath(level);
            if (string.IsNullOrWhiteSpace(path))
                return;

            BackgroundImage = new Bitmap(path);
        }

        private void SyncWaypointsToLevel()
        {
            var path = _editorSession.SelectedPath;
            if (path is null)
                return;

            path.Waypoints = Waypoints
                .Select(point => new GridPoint { Col = point.Col, Row = point.Row })
                .ToList();

            _syncingWaypoints = true;
            try
            {
                RefreshPathLayers();
                _editorSession.NotifyPathsChanged();
            }
            finally
            {
                _syncingWaypoints = false;
            }
        }

        private void PushUndoSnapshot() => _pathUndo.Push(CloneWaypoints());

        private List<GridPoint> CloneWaypoints() =>
            Waypoints.Select(point => new GridPoint { Col = point.Col, Row = point.Row }).ToList();

        private void ApplyWaypoints(IReadOnlyList<GridPoint> snapshot)
        {
            Waypoints.Clear();
            foreach (var point in snapshot)
                Waypoints.Add(new GridPoint { Col = point.Col, Row = point.Row });

            SyncWaypointsToLevel();
        }

        private void UpdateUndoState()
        {
            CanUndo = _pathUndo.CanUndo;
            CanRedo = _pathUndo.CanRedo;
        }
    }
}
