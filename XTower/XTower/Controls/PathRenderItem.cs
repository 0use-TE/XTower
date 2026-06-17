using Avalonia.Media;
using System.Collections.Generic;
using XTower.Models.Content;

namespace XTower.Controls
{
    public sealed class PathRenderItem
    {
        public IList<GridPoint>? Waypoints { get; init; }

        public Color Color { get; init; } = Colors.OrangeRed;

        public bool IsSelected { get; init; }
    }
}
