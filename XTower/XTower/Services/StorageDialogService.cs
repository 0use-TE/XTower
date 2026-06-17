using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Linq;
using System.Threading.Tasks;

namespace XTower.Services
{
    internal static class StorageDialogService
    {
        public static async Task<string?> PickFolderAsync(string title)
        {
            var topLevel = ResolveTopLevel();
            if (topLevel?.StorageProvider.CanPickFolder != true)
                return null;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
            });

            return folders.FirstOrDefault()?.TryGetLocalPath();
        }

        public static async Task<string?> PickImageAsync(string title)
        {
            var topLevel = ResolveTopLevel();
            if (topLevel?.StorageProvider.CanOpen != true)
                return null;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("图片")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"],
                    },
                ],
            });

            return files.FirstOrDefault()?.TryGetLocalPath();
        }

        private static TopLevel? ResolveTopLevel()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;

            return null;
        }
    }
}
