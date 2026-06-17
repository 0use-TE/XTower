using System;
using Avalonia.Media;
using XTower.Models.Content;

namespace XTower.Services
{
    internal static class PathColorHelper
    {
        public static Color ToColor(string? hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return fallback;

            if (Color.TryParse(hex, out var color))
                return color;

            return fallback;
        }
    }
}
