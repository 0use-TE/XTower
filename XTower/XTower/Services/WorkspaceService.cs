using System;
using System.IO;
using System.Text.Json;
using XTower.Models.Content;
using XTower.Persistence;

namespace XTower.Services
{
    internal sealed class WorkspaceService : IWorkspaceService
    {
        private const string WorkspaceStorageName = "workspace.json";

        private readonly IDataPersistence _dataPersistence;
        private string? _rootPath;

        public bool IsOpen => !string.IsNullOrWhiteSpace(_rootPath);

        public string? RootPath => _rootPath;

        public string ContentPath => IsOpen ? Path.Combine(_rootPath!, "Content") : string.Empty;

        public event EventHandler? WorkspaceChanged;

        public WorkspaceService(IDataPersistence dataPersistence)
        {
            _dataPersistence = dataPersistence;
        }

        public bool TryOpen(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                return false;

            _rootPath = Path.GetFullPath(rootPath);
            EnsureContentStructure();
            _dataPersistence.Save(
                WorkspaceStorageName,
                new WorkspaceStorageModel { RootPath = _rootPath },
                WorkspaceJsonContext.Default.WorkspaceStorageModel);
            WorkspaceChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void EnsureInitialized()
        {
            if (IsOpen)
                return;

            var stored = _dataPersistence.Load(WorkspaceStorageName, WorkspaceJsonContext.Default.WorkspaceStorageModel);
            if (!string.IsNullOrWhiteSpace(stored?.RootPath) && Directory.Exists(stored.RootPath))
            {
                TryOpen(stored.RootPath);
                return;
            }

            var discovered = DiscoverWorkspaceRoot();
            if (discovered != null)
                TryOpen(discovered);
        }

        public string GetContentFilePath(string relativePath) =>
            Path.Combine(ContentPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        public string ToContentRelativePath(string absolutePath)
        {
            var relative = Path.GetRelativePath(ContentPath, absolutePath);
            return relative.Replace(Path.DirectorySeparatorChar, '/');
        }

        private static string? DiscoverWorkspaceRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                var contentDir = Path.Combine(directory.FullName, "Content");
                if (Directory.Exists(contentDir))
                    return directory.FullName;

                if (File.Exists(Path.Combine(directory.FullName, "XTower.slnx")) ||
                    File.Exists(Path.Combine(directory.FullName, "XTower.sln")))
                    return directory.FullName;

                directory = directory.Parent;
            }

            return null;
        }

        private void EnsureContentStructure()
        {
            Directory.CreateDirectory(GetContentFilePath("levels"));
            Directory.CreateDirectory(GetContentFilePath("assets/levels"));
            Directory.CreateDirectory(GetContentFilePath("monsters"));
            Directory.CreateDirectory(GetContentFilePath("turrets"));
            Directory.CreateDirectory(GetContentFilePath("music"));
            Directory.CreateDirectory(GetContentFilePath("schemas"));

            var projectFile = GetContentFilePath("project.json");
            if (!File.Exists(projectFile))
            {
                var project = new ProjectConfig();
                WriteJson(projectFile, project, ContentJsonContext.Default.ProjectConfig);
            }

            var indexFile = GetContentFilePath("levels/_index.json");
            if (!File.Exists(indexFile))
            {
                var index = new LevelIndex();
                WriteJson(indexFile, index, ContentJsonContext.Default.LevelIndex);
            }
        }

        private static void WriteJson<T>(string path, T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
            where T : class
        {
            var json = JsonSerializer.Serialize(value, typeInfo);
            File.WriteAllText(path, json);
        }
    }
}
