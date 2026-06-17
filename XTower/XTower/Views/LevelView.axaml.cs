using Avalonia.Controls;
using XTower.Models.Content;
using XTower.ViewModels;

namespace XTower.Views;

public partial class LevelView : UserControl
{
    public LevelView()
    {
        InitializeComponent();
        LevelCanvas.CellClicked += OnCellClicked;
    }

    private void OnCellClicked(object? sender, GridPoint point)
    {
        Focus();
        if (DataContext is LevelViewModel viewModel)
            viewModel.HandleCellClickCommand.Execute(point);
    }
}
