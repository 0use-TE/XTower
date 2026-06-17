using System;
using XTower.Models.Content;

namespace XTower.Services
{
    internal interface ILevelEditorSession
    {
        LevelDefinition? CurrentLevel { get; }

        ProjectConfig? Project { get; }

        PathDefinition? SelectedPath { get; }

        string? SelectedPathId { get; }

        event EventHandler? LevelChanged;

        event EventHandler? ProjectChanged;

        event EventHandler? SelectedPathChanged;

        event EventHandler? PathsChanged;

        event EventHandler? LevelSettingsChanged;

        void RefreshProject();

        void LoadLevel(string levelId);

        void SaveCurrentLevel();

        void SetProject(ProjectConfig project);

        void SelectPath(string pathId);

        PathDefinition AddPath(string? name = null);

        void DeletePath(string pathId);

        void NotifyPathsChanged();

        void NotifyLevelSettingsChanged();
    }
}
