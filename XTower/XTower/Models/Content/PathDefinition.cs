using System.Collections.Generic;

namespace XTower.Models.Content
{
    internal sealed class PathDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = "#FF5722";

        public List<GridPoint> Waypoints { get; set; } = [];
    }

    /// <summary>Legacy single-path field; migrated to <see cref="LevelDefinition.Paths"/> on load.</summary>
    internal sealed class PathConfig
    {
        public List<GridPoint> Waypoints { get; set; } = [];
    }
}
