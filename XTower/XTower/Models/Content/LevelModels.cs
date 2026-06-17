using System.Collections.Generic;

namespace XTower.Models.Content
{
    internal sealed class DesignResolution
    {
        public int Width { get; set; } = 1920;

        public int Height { get; set; } = 1080;
    }

    internal sealed class ProjectConfig
    {
        public string Name { get; set; } = "XTower";

        public DesignResolution DesignResolution { get; set; } = new();

        public GridConfig DefaultGrid { get; set; } = new();
    }

    public sealed class GridPoint
    {
        public int Col { get; set; }

        public int Row { get; set; }
    }

    internal sealed class BackgroundConfig
    {
        public string Image { get; set; } = string.Empty;

        public string Stretch { get; set; } = "Fill";
    }

    internal sealed class LevelMeta
    {
        public int StartingGold { get; set; } = 200;

        public int StartingLives { get; set; } = 20;
    }

    internal sealed class LevelDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public GridConfig Grid { get; set; } = new();

        public BackgroundConfig? Background { get; set; }

        public List<PathDefinition> Paths { get; set; } = [];

        public PathConfig? Path { get; set; }

        public LevelMeta Meta { get; set; } = new();
    }

    internal sealed class LevelIndex
    {
        public List<string> Levels { get; set; } = [];
    }
}
