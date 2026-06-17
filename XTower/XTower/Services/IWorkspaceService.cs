using System;

namespace XTower.Services
{
    internal interface IWorkspaceService
    {
        bool IsOpen { get; }

        string? RootPath { get; }

        string ContentPath { get; }

        event EventHandler? WorkspaceChanged;

        bool TryOpen(string rootPath);

        void EnsureInitialized();

        string GetContentFilePath(string relativePath);

        string ToContentRelativePath(string absolutePath);
    }
}
