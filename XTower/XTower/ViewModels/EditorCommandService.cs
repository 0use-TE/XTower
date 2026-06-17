using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using XTower.Services;

namespace XTower.ViewModels
{
    internal interface IEditorCommandService : INotifyPropertyChanged
    {
        string StatusText { get; }

        bool IsPathMode { get; set; }

        IRelayCommand SaveCommand { get; }

        IRelayCommand UndoCommand { get; }

        IRelayCommand RedoCommand { get; }

        IRelayCommand ClearPathCommand { get; }
    }

    internal sealed partial class EditorCommandService : ObservableObject, IEditorCommandService
    {
        private readonly LevelViewModel _level;
        private readonly ILevelEditorSession _session;

        [ObservableProperty]
        private string _statusText = "未选择关卡";

        public EditorCommandService(LevelViewModel level, ILevelEditorSession session)
        {
            _level = level;
            _session = session;

            _level.PropertyChanged += OnLevelPropertyChanged;
            _session.LevelChanged += (_, _) => RefreshStatus();
            _session.SelectedPathChanged += (_, _) => RefreshStatus();
            _session.PathsChanged += (_, _) => RefreshStatus();

            RefreshStatus();
        }

        public bool IsPathMode
        {
            get => _level.IsPathMode;
            set => _level.IsPathMode = value;
        }

        IRelayCommand IEditorCommandService.SaveCommand => SaveCommand;

        IRelayCommand IEditorCommandService.UndoCommand => UndoCommand;

        IRelayCommand IEditorCommandService.RedoCommand => RedoCommand;

        IRelayCommand IEditorCommandService.ClearPathCommand => ClearPathCommand;

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save() => _level.SaveLevelCommand.Execute(null);

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo() => _level.UndoPathCommand.Execute(null);

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo() => _level.RedoPathCommand.Execute(null);

        [RelayCommand(CanExecute = nameof(CanClearPath))]
        private void ClearPath() => _level.ClearPathCommand.Execute(null);

        private bool CanSave => _level.HasLevel;

        private bool CanUndo => _level.CanUndo;

        private bool CanRedo => _level.CanRedo;

        private bool CanClearPath => _level.HasLevel && _session.SelectedPath is not null;

        private void OnLevelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(LevelViewModel.HasLevel):
                    SaveCommand.NotifyCanExecuteChanged();
                    ClearPathCommand.NotifyCanExecuteChanged();
                    RefreshStatus();
                    break;
                case nameof(LevelViewModel.CanUndo):
                    UndoCommand.NotifyCanExecuteChanged();
                    break;
                case nameof(LevelViewModel.CanRedo):
                    RedoCommand.NotifyCanExecuteChanged();
                    break;
                case nameof(LevelViewModel.IsPathMode):
                    OnPropertyChanged(nameof(IsPathMode));
                    break;
            }
        }

        private void RefreshStatus()
        {
            var level = _session.CurrentLevel;
            if (level is null)
            {
                StatusText = "未选择关卡";
                SaveCommand.NotifyCanExecuteChanged();
                ClearPathCommand.NotifyCanExecuteChanged();
                return;
            }

            var path = _session.SelectedPath;
            StatusText = path is null
                ? $"{level.Name} ({level.Id})"
                : $"{level.Name} ({level.Id}) · {path.Name}";

            SaveCommand.NotifyCanExecuteChanged();
            ClearPathCommand.NotifyCanExecuteChanged();
        }
    }
}
