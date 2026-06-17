using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using XTower.Models.Content;

namespace XTower.Controls
{
    public sealed class LevelCanvasControl : Control
    {
        public const double AxisLabelMargin = 22;

        private static readonly Typeface LabelTypeface = new("Segoe UI");
        private static readonly IBrush BoundaryLabelBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0));
        private static readonly IBrush BoundaryLabelShadowBrush = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));

        public static readonly StyledProperty<int> ColumnsProperty =
            AvaloniaProperty.Register<LevelCanvasControl, int>(nameof(Columns), 32);

        public static readonly StyledProperty<int> RowsProperty =
            AvaloniaProperty.Register<LevelCanvasControl, int>(nameof(Rows), 18);

        public static readonly StyledProperty<double> CellSizeProperty =
            AvaloniaProperty.Register<LevelCanvasControl, double>(nameof(CellSize), 32);

        public static readonly StyledProperty<IImage?> BackgroundImageProperty =
            AvaloniaProperty.Register<LevelCanvasControl, IImage?>(nameof(BackgroundImage));

        public static readonly StyledProperty<IList<PathRenderItem>?> PathsProperty =
            AvaloniaProperty.Register<LevelCanvasControl, IList<PathRenderItem>?>(nameof(Paths));

        public static readonly StyledProperty<bool> IsPathModeProperty =
            AvaloniaProperty.Register<LevelCanvasControl, bool>(nameof(IsPathMode));

        public event EventHandler<GridPoint>? CellClicked;

        private IList<PathRenderItem>? _subscribedPaths;

        static LevelCanvasControl()
        {
            AffectsRender<LevelCanvasControl>(
                ColumnsProperty,
                RowsProperty,
                CellSizeProperty,
                BackgroundImageProperty,
                PathsProperty,
                IsPathModeProperty);
        }

        public int Columns
        {
            get => GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public int Rows
        {
            get => GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public double CellSize
        {
            get => GetValue(CellSizeProperty);
            set => SetValue(CellSizeProperty, value);
        }

        public IImage? BackgroundImage
        {
            get => GetValue(BackgroundImageProperty);
            set => SetValue(BackgroundImageProperty, value);
        }

        public IList<PathRenderItem>? Paths
        {
            get => GetValue(PathsProperty);
            set => SetValue(PathsProperty, value);
        }

        public bool IsPathMode
        {
            get => GetValue(IsPathModeProperty);
            set => SetValue(IsPathModeProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize) =>
            new(GetTotalWidth(), GetTotalHeight());

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == PathsProperty)
            {
                UnsubscribePaths(_subscribedPaths);
                _subscribedPaths = Paths;

                if (_subscribedPaths is INotifyCollectionChanged notify)
                    notify.CollectionChanged += OnPathsChanged;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (!IsPathMode || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;

            var position = e.GetPosition(this);
            var col = (int)((position.X - AxisLabelMargin) / CellSize);
            var row = (int)((position.Y - AxisLabelMargin) / CellSize);

            if (col < 0 || col >= Columns || row < 0 || row >= Rows)
                return;

            CellClicked?.Invoke(this, new GridPoint { Col = col, Row = row });
            e.Handled = true;
        }

        public override void Render(DrawingContext context)
        {
            var gridLeft = AxisLabelMargin;
            var gridTop = AxisLabelMargin;
            var gridWidth = Columns * CellSize;
            var gridHeight = Rows * CellSize;
            var gridBounds = new Rect(gridLeft, gridTop, gridWidth, gridHeight);
            var totalBounds = new Rect(0, 0, GetTotalWidth(), GetTotalHeight());

            context.FillRectangle(new SolidColorBrush(Color.FromRgb(32, 32, 32)), totalBounds);

            if (BackgroundImage != null)
                context.DrawImage(BackgroundImage, new Rect(0, 0, BackgroundImage.Size.Width, BackgroundImage.Size.Height), gridBounds);

            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 1);
            for (var col = 0; col <= Columns; col++)
            {
                var x = gridLeft + col * CellSize;
                context.DrawLine(gridPen, new Point(x, gridTop), new Point(x, gridTop + gridHeight));
            }

            for (var row = 0; row <= Rows; row++)
            {
                var y = gridTop + row * CellSize;
                context.DrawLine(gridPen, new Point(gridLeft, y), new Point(gridLeft + gridWidth, y));
            }

            DrawOrigin(context, gridLeft, gridTop);
            DrawPaths(context, gridLeft, gridTop);
            DrawGridBoundary(context, gridBounds);
            DrawBoundaryLabels(context, gridLeft, gridTop, gridWidth, gridHeight);
        }

        private static double GetTotalWidth(double cellSize, int columns) =>
            columns * cellSize + AxisLabelMargin * 2;

        private static double GetTotalHeight(double cellSize, int rows) =>
            rows * cellSize + AxisLabelMargin * 2;

        private double GetTotalWidth() => GetTotalWidth(CellSize, Columns);

        private double GetTotalHeight() => GetTotalHeight(CellSize, Rows);

        private void DrawOrigin(DrawingContext context, double gridLeft, double gridTop)
        {
            var origin = new Point(gridLeft, gridTop);
            var axisLength = Math.Min(CellSize * 3, 96);
            var xPen = new Pen(Brushes.Red, 2);
            var yPen = new Pen(Brushes.LimeGreen, 2);

            context.DrawLine(xPen, origin, new Point(origin.X + axisLength, origin.Y));
            context.DrawLine(yPen, origin, new Point(origin.X, origin.Y + axisLength));

            var originGeometry = new StreamGeometry();
            using (var builder = originGeometry.Open())
            {
                builder.BeginFigure(origin, isFilled: true);
                builder.LineTo(new Point(origin.X + 8, origin.Y));
                builder.LineTo(new Point(origin.X, origin.Y + 8));
                builder.EndFigure(isClosed: true);
            }

            context.DrawGeometry(Brushes.White, new Pen(Brushes.Black, 1), originGeometry);
            DrawShadowedText(context, "O(0,0)", new Point(origin.X + 10, origin.Y + 2), 11, Brushes.White);
        }

        private void DrawGridBoundary(DrawingContext context, Rect bounds)
        {
            var boundaryPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 255, 215, 0)), 3);
            context.DrawRectangle(null, boundaryPen, bounds);
        }

        private void DrawBoundaryLabels(DrawingContext context, double gridLeft, double gridTop, double gridWidth, double gridHeight)
        {
            const double fontSize = 10;
            var labelStep = CellSize >= 20 ? 1 : CellSize >= 12 ? 2 : 4;

            for (var col = 0; col < Columns; col += labelStep)
            {
                var centerX = gridLeft + (col + 0.5) * CellSize;
                var label = col.ToString(CultureInfo.InvariantCulture);
                DrawShadowedText(
                    context,
                    label,
                    new Point(centerX, gridTop - fontSize - 4),
                    fontSize,
                    BoundaryLabelBrush,
                    horizontalAlignment: TextAlignment.Center);

                DrawShadowedText(
                    context,
                    label,
                    new Point(centerX, gridTop + gridHeight + 4),
                    fontSize,
                    BoundaryLabelBrush,
                    horizontalAlignment: TextAlignment.Center);
            }

            for (var row = 0; row < Rows; row += labelStep)
            {
                var centerY = gridTop + (row + 0.5) * CellSize;
                var label = row.ToString(CultureInfo.InvariantCulture);
                DrawShadowedText(
                    context,
                    label,
                    new Point(gridLeft - 4, centerY - fontSize * 0.5),
                    fontSize,
                    BoundaryLabelBrush,
                    horizontalAlignment: TextAlignment.Right);

                DrawShadowedText(
                    context,
                    label,
                    new Point(gridLeft + gridWidth + 4, centerY - fontSize * 0.5),
                    fontSize,
                    BoundaryLabelBrush,
                    horizontalAlignment: TextAlignment.Left);
            }
        }

        private void DrawPaths(DrawingContext context, double gridLeft, double gridTop)
        {
            var paths = Paths;
            if (paths is null)
                return;

            foreach (var path in paths)
            {
                if (path.IsSelected || path.Waypoints is not { Count: > 0 })
                    continue;

                DrawSinglePath(context, path, gridLeft, gridTop, dimmed: true, showCoordinateLabels: false);
            }

            foreach (var path in paths)
            {
                if (!path.IsSelected || path.Waypoints is not { Count: > 0 })
                    continue;

                DrawSinglePath(context, path, gridLeft, gridTop, dimmed: false, showCoordinateLabels: true);
            }
        }

        private void DrawSinglePath(
            DrawingContext context,
            PathRenderItem path,
            double gridLeft,
            double gridTop,
            bool dimmed,
            bool showCoordinateLabels)
        {
            var waypoints = path.Waypoints!;
            var baseColor = path.Color;
            var color = dimmed
                ? new SolidColorBrush(Color.FromArgb(140, baseColor.R, baseColor.G, baseColor.B))
                : new SolidColorBrush(baseColor);
            const double lineWidth = 3;
            var pathPen = new Pen(color, lineWidth);

            for (var i = 0; i < waypoints.Count - 1; i++)
            {
                var start = ToPixelCenter(waypoints[i], gridLeft, gridTop);
                var end = ToPixelCenter(waypoints[i + 1], gridLeft, gridTop);
                context.DrawLine(pathPen, start, end);
                DrawDirectionArrow(context, start, end, dimmed
                    ? Color.FromArgb(140, baseColor.R, baseColor.G, baseColor.B)
                    : baseColor);
            }

            foreach (var waypoint in waypoints)
            {
                var center = ToPixelCenter(waypoint, gridLeft, gridTop);
                var radius = path.IsSelected ? 6 : 5;
                context.DrawEllipse(
                    color,
                    new Pen(Brushes.White, 2),
                    center,
                    radius,
                    radius);

                if (showCoordinateLabels)
                {
                    var coordinateLabel = $"({waypoint.Col},{waypoint.Row})";
                    DrawShadowedText(
                        context,
                        coordinateLabel,
                        new Point(center.X, center.Y + radius + 3),
                        10,
                        BoundaryLabelBrush,
                        horizontalAlignment: TextAlignment.Center);
                }
            }
        }

        private Point ToPixelCenter(GridPoint point, double gridLeft, double gridTop) =>
            new(gridLeft + (point.Col + 0.5) * CellSize, gridTop + (point.Row + 0.5) * CellSize);

        private static void DrawShadowedText(
            DrawingContext context,
            string text,
            Point position,
            double fontSize,
            IBrush foreground,
            TextAlignment horizontalAlignment = TextAlignment.Left)
        {
            var formatted = CreateFormattedText(text, fontSize, foreground);
            var drawPoint = horizontalAlignment switch
            {
                TextAlignment.Center => new Point(position.X - formatted.Width * 0.5, position.Y),
                TextAlignment.Right => new Point(position.X - formatted.Width, position.Y),
                _ => position,
            };

            var shadow = CreateFormattedText(text, fontSize, BoundaryLabelShadowBrush);
            context.DrawText(shadow, drawPoint + new Vector(1, 1));
            context.DrawText(formatted, drawPoint);
        }

        private static FormattedText CreateFormattedText(string text, double fontSize, IBrush foreground) =>
            new(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                LabelTypeface,
                fontSize,
                foreground);

        private static void DrawDirectionArrow(DrawingContext context, Point start, Point end, Color color)
        {
            var direction = new Vector(end.X - start.X, end.Y - start.Y);
            if (direction.X * direction.X + direction.Y * direction.Y < 1)
                return;

            direction = direction.Normalize();
            var midpoint = new Point(
                (start.X + end.X) * 0.5,
                (start.Y + end.Y) * 0.5);
            var normal = new Vector(-direction.Y, direction.X);
            var tip = midpoint + direction * 10;
            var left = midpoint - direction * 6 + normal * 5;
            var right = midpoint - direction * 6 - normal * 5;

            var geometry = new StreamGeometry();
            using (var builder = geometry.Open())
            {
                builder.BeginFigure(tip, isFilled: true);
                builder.LineTo(left);
                builder.LineTo(right);
                builder.EndFigure(isClosed: true);
            }

            context.DrawGeometry(new SolidColorBrush(color), null, geometry);
        }

        private void OnPathsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            InvalidateVisual();

        private void UnsubscribePaths(IList<PathRenderItem>? paths)
        {
            if (paths is INotifyCollectionChanged notify)
                notify.CollectionChanged -= OnPathsChanged;
        }
    }
}
