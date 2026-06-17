using Avalonia.Controls;
using Avalonia.Interactivity;

namespace XTower.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e) => Focus();
}
