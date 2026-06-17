using System;
using XTower.Models.Content;

namespace XTower.Services
{
    internal static class PathDefaults
    {
        private static readonly string[] Palette =
        [
            "#F44336",
            "#2196F3",
            "#4CAF50",
            "#FF9800",
            "#9C27B0",
            "#00BCD4",
            "#E91E63",
            "#795548",
        ];

        public static PathDefinition Create(string id, string name, int colorIndex)
        {
            return new PathDefinition
            {
                Id = id,
                Name = name,
                Color = Palette[colorIndex % Palette.Length],
            };
        }

        public static void EnsurePaths(LevelDefinition level)
        {
            if (level.Paths.Count > 0)
                return;

            if (level.Path is { Waypoints.Count: > 0 } legacy)
            {
                level.Paths.Add(new PathDefinition
                {
                    Id = "path-1",
                    Name = "路径 1",
                    Color = Palette[0],
                    Waypoints = legacy.Waypoints,
                });
                level.Path = null;
                return;
            }

            level.Paths.Add(Create("path-1", "路径 1", 0));
        }

        public static string NewPathId(LevelDefinition level)
        {
            for (var i = 1; i < 1000; i++)
            {
                var id = $"path-{i}";
                if (level.Paths.TrueForAll(path => !string.Equals(path.Id, id, StringComparison.Ordinal)))
                    return id;
            }

            return Guid.NewGuid().ToString("N")[..8];
        }
    }
}
