using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using XTower.Models.Content;
using XTower.Services;

namespace XTower.ViewModels
{
    internal partial class LevelPathsViewModel : DockViewModel
    {
        private readonly ILevelEditorSession _editorSession;
        private readonly IEditorCommandService _editorCommands;

        public override string Id => "level-paths";

        public override string Header => "路径";

        public override DockPosition DockPosition => DockPosition.CenterBottom;

        [ObservableProperty]
        private bool _hasLevel;

        [ObservableProperty]
        private bool _hasSelectedPath;

        [ObservableProperty]
        private PathListItemViewModel? _selectedPath;

        [ObservableProperty]
        private string _selectedPathName = string.Empty;

        [ObservableProperty]
        private string _selectedPathColor = "#FF5722";

        public ObservableCollection<PathListItemViewModel> Paths { get; } = [];

        public ObservableCollection<WaypointListItemViewModel> SelectedPathWaypoints { get; } = [];

        private bool _syncingSelection;

        private bool _syncingWaypoints;

        private bool _committingWaypoints;

        [ObservableProperty]
        private bool _canDeletePath;

        public LevelPathsViewModel(ILevelEditorSession editorSession, IEditorCommandService editorCommands)
        {
            _editorSession = editorSession;
            _editorCommands = editorCommands;
            _editorSession.LevelChanged += (_, _) => RefreshAll();
            _editorSession.SelectedPathChanged += (_, _) => SyncSelectionFromSession();
            _editorSession.PathsChanged += (_, _) =>
            {
                RefreshPathList();
                if (!_committingWaypoints)
                    SyncWaypointsFromSession();
            };
            RefreshAll();
        }

        partial void OnSelectedPathChanged(PathListItemViewModel? value)
        {
            if (_syncingSelection || value is null || !HasLevel)
                return;

            if (!string.Equals(_editorSession.SelectedPathId, value.Id, StringComparison.Ordinal))
                _editorSession.SelectPath(value.Id);
        }

        private void RefreshAll()
        {
            var level = _editorSession.CurrentLevel;
            HasLevel = level is not null;
            RefreshPathList();
            SyncSelectionFromSession();
        }

        private void RefreshPathList()
        {
            var level = _editorSession.CurrentLevel;
            var selectedId = _editorSession.SelectedPathId;
            Paths.Clear();

            if (level is null)
                return;

            foreach (var path in level.Paths)
            {
                Paths.Add(new PathListItemViewModel(
                    path.Id,
                    path.Name,
                    path.Color,
                    path.Waypoints.Count));
            }

            SelectedPath = Paths.FirstOrDefault(path => string.Equals(path.Id, selectedId, StringComparison.Ordinal))
                ?? Paths.FirstOrDefault();

            CanDeletePath = Paths.Count > 1;
        }

        public IRelayCommand ClearPathCommand => _editorCommands.ClearPathCommand;

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
            if (!CanDeletePath || SelectedPath is null)
                return;

            _editorSession.DeletePath(SelectedPath.Id);
        }

        [RelayCommand]
        private void AddWaypoint()
        {
            if (!HasSelectedPath)
                return;

            var last = SelectedPathWaypoints.LastOrDefault();
            var x = last?.X ?? 0;
            var y = last?.Y ?? 0;
            SelectedPathWaypoints.Add(new WaypointListItemViewModel(
                SelectedPathWaypoints.Count,
                x,
                y,
                CommitWaypointChanges));
            ReindexWaypoints();
            CommitWaypointChanges();
        }

        private void SyncSelectionFromSession()
        {
            _syncingSelection = true;
            try
            {
                var path = _editorSession.SelectedPath;
                HasSelectedPath = path is not null;

                if (path is null)
                {
                    SelectedPath = null;
                    SelectedPathName = string.Empty;
                    SelectedPathColor = "#FF5722";
                    SelectedPathWaypoints.Clear();
                    return;
                }

                SelectedPath = Paths.FirstOrDefault(item => string.Equals(item.Id, path.Id, StringComparison.Ordinal));
                SelectedPathName = path.Name;
                SelectedPathColor = path.Color;
                SyncWaypointsFromSession();
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        private void SyncWaypointsFromSession()
        {
            var path = _editorSession.SelectedPath;
            _syncingWaypoints = true;
            try
            {
                SelectedPathWaypoints.Clear();
                if (path is null)
                    return;

                for (var i = 0; i < path.Waypoints.Count; i++)
                {
                    var waypoint = path.Waypoints[i];
                    SelectedPathWaypoints.Add(new WaypointListItemViewModel(
                        i,
                        waypoint.Col,
                        waypoint.Row,
                        CommitWaypointChanges));
                }
            }
            finally
            {
                _syncingWaypoints = false;
            }
        }

        private void CommitWaypointChanges()
        {
            if (_syncingWaypoints)
                return;

            var path = _editorSession.SelectedPath;
            if (path is null)
                return;

            path.Waypoints = SelectedPathWaypoints
                .Select(waypoint => new GridPoint { Col = waypoint.X, Row = waypoint.Y })
                .ToList();

            RefreshPathList();
            _committingWaypoints = true;
            try
            {
                _editorSession.NotifyPathsChanged();
            }
            finally
            {
                _committingWaypoints = false;
            }
        }

        [RelayCommand]
        private void RemoveWaypoint(WaypointListItemViewModel? waypoint)
        {
            if (waypoint is null || !SelectedPathWaypoints.Contains(waypoint))
                return;

            SelectedPathWaypoints.Remove(waypoint);
            ReindexWaypoints();
            CommitWaypointChanges();
        }

        private void ReindexWaypoints()
        {
            for (var i = 0; i < SelectedPathWaypoints.Count; i++)
                SelectedPathWaypoints[i].SetIndex(i);
        }
    }

    internal sealed class PathListItemViewModel
    {
        public PathListItemViewModel(string id, string name, string color, int waypointCount)
        {
            Id = id;
            Name = name;
            Color = color;
            WaypointCount = waypointCount;
        }

        public string Id { get; }

        public string Name { get; }

        public string Color { get; }

        public int WaypointCount { get; }

        public string Display => $"{Name}  ·  {WaypointCount} 点";
    }

    internal partial class WaypointListItemViewModel : ObservableObject
    {
        private readonly Action _onChanged;

        private bool _syncing;

        public WaypointListItemViewModel(int index, int x, int y, Action onChanged)
        {
            _onChanged = onChanged;
            _index = index;
            _x = x;
            _y = y;
        }

        [ObservableProperty]
        private int _index;

        [ObservableProperty]
        private int _x;

        [ObservableProperty]
        private int _y;

        public string IndexLabel => Index.ToString();

        public void SetIndex(int index)
        {
            _syncing = true;
            try
            {
                Index = index;
                OnPropertyChanged(nameof(IndexLabel));
            }
            finally
            {
                _syncing = false;
            }
        }

        partial void OnXChanged(int value)
        {
            if (!_syncing)
                _onChanged();
        }

        partial void OnYChanged(int value)
        {
            if (!_syncing)
                _onChanged();
        }
    }
}
