namespace XTower.ViewModels
{
    internal partial class MusicViewModel : DockViewModel
    {
        public override string Id => "music";

        public override string Header => "音乐";

        public override DockPosition DockPosition => DockPosition.Right;
    }
}
