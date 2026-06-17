using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using XTower.Models.Content;

namespace XTower.Services
{
    internal sealed class ContentStore : IContentStore
    {
        private readonly IWorkspaceService _workspace;

        public ContentStore(IWorkspaceService workspace)
        {
            _workspace = workspace;
        }

        public ProjectConfig LoadProject()
        {
            EnsureWorkspace();
            var path = _workspace.GetContentFilePath("project.json");
            return ReadJson(path, ContentJsonContext.Default.ProjectConfig) ?? new ProjectConfig();
        }

        public void SaveProject(ProjectConfig project)
        {
            EnsureWorkspace();
            WriteJson(_workspace.GetContentFilePath("project.json"), project, ContentJsonContext.Default.ProjectConfig);
        }

        public IReadOnlyList<string> ListLevelIds()
        {
            EnsureWorkspace();
            var index = ReadJson(_workspace.GetContentFilePath("levels/_index.json"), ContentJsonContext.Default.LevelIndex)
                ?? new LevelIndex();
            return index.Levels;
        }

        public LevelDefinition? LoadLevel(string levelId)
        {
            EnsureWorkspace();
            var path = _workspace.GetContentFilePath($"levels/{levelId}.json");
            if (!File.Exists(path))
                return null;

            return ReadJson(path, ContentJsonContext.Default.LevelDefinition) is { } level
                ? MigrateLevel(level)
                : null;
        }

        private static LevelDefinition MigrateLevel(LevelDefinition level)
        {
            PathDefaults.EnsurePaths(level);
            return level;
        }

        public void SaveLevel(LevelDefinition level)
        {
            EnsureWorkspace();
            WriteJson(_workspace.GetContentFilePath($"levels/{level.Id}.json"), level, ContentJsonContext.Default.LevelDefinition);

            var indexPath = _workspace.GetContentFilePath("levels/_index.json");
            var index = ReadJson(indexPath, ContentJsonContext.Default.LevelIndex) ?? new LevelIndex();
            if (!index.Levels.Contains(level.Id, StringComparer.Ordinal))
            {
                index.Levels.Add(level.Id);
                index.Levels.Sort(StringComparer.Ordinal);
            }

            WriteJson(indexPath, index, ContentJsonContext.Default.LevelIndex);
        }

        public LevelDefinition CreateLevel(string levelId, string? displayName = null)
        {
            EnsureWorkspace();
            var project = LoadProject();
            var level = new LevelDefinition
            {
                Id = levelId,
                Name = displayName ?? levelId,
                Grid = new GridConfig
                {
                    Columns = project.DefaultGrid.Columns,
                    Rows = project.DefaultGrid.Rows,
                    CellSize = project.DefaultGrid.CellSize,
                },
            };

            PathDefaults.EnsurePaths(level);
            SaveLevel(level);
            return level;
        }

        public void DeleteLevel(string levelId)
        {
            EnsureWorkspace();
            var levelPath = _workspace.GetContentFilePath($"levels/{levelId}.json");
            if (File.Exists(levelPath))
                File.Delete(levelPath);

            var indexPath = _workspace.GetContentFilePath("levels/_index.json");
            var index = ReadJson(indexPath, ContentJsonContext.Default.LevelIndex) ?? new LevelIndex();
            index.Levels.RemoveAll(id => string.Equals(id, levelId, StringComparison.Ordinal));
            WriteJson(indexPath, index, ContentJsonContext.Default.LevelIndex);

            var assetDir = _workspace.GetContentFilePath($"assets/levels/{levelId}");
            if (Directory.Exists(assetDir))
                Directory.Delete(assetDir, recursive: true);
        }

        public string ImportLevelBackground(string levelId, string sourceImagePath)
        {
            EnsureWorkspace();
            var assetDir = _workspace.GetContentFilePath($"assets/levels/{levelId}");
            Directory.CreateDirectory(assetDir);

            var extension = Path.GetExtension(sourceImagePath);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".png";

            var targetFile = Path.Combine(assetDir, "bg" + extension.ToLowerInvariant());
            File.Copy(sourceImagePath, targetFile, overwrite: true);
            return _workspace.ToContentRelativePath(targetFile);
        }

        public string? ResolveBackgroundAbsolutePath(LevelDefinition level)
        {
            if (level.Background == null || string.IsNullOrWhiteSpace(level.Background.Image))
                return null;

            EnsureWorkspace();
            return _workspace.GetContentFilePath(level.Background.Image);
        }

        private void EnsureWorkspace()
        {
            if (!_workspace.IsOpen)
                throw new InvalidOperationException("工作区未打开。");
        }

        private static T? ReadJson<T>(string path, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
            where T : class
        {
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize(json, typeInfo);
        }

        private static void WriteJson<T>(string path, T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
            where T : class
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(value, typeInfo);
            File.WriteAllText(path, json);
        }
    }
}
