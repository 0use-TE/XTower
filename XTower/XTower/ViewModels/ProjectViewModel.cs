namespace XTower.ViewModels
{
    internal partial class ProjectViewModel : DockViewModel
    {
        public override string Id => "project";

        public override string Header => "项目";

        public override DockPosition DockPosition => DockPosition.Left;
    }
}
