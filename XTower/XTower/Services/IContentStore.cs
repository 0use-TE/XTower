using System.Collections.Generic;
using XTower.Models.Content;

namespace XTower.Services
{
    internal interface IContentStore
    {
        ProjectConfig LoadProject();

        void SaveProject(ProjectConfig project);

        IReadOnlyList<string> ListLevelIds();

        LevelDefinition? LoadLevel(string levelId);

        void SaveLevel(LevelDefinition level);

        LevelDefinition CreateLevel(string levelId, string? displayName = null);

        void DeleteLevel(string levelId);

        string ImportLevelBackground(string levelId, string sourceImagePath);

        string? ResolveBackgroundAbsolutePath(LevelDefinition level);
    }
}
