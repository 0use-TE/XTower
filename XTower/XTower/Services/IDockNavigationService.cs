namespace XTower.Services
{
    internal interface IDockNavigationService
    {
        void ShowTab<TTab>() where TTab : ViewModels.DockViewModel;
    }
}
