namespace XTower.ViewModels
{
    internal partial class TurretViewModel : DockViewModel
    {
        public override string Id => "turret";

        public override string Header => "炮塔";

        public override DockPosition DockPosition => DockPosition.Right;
    }
}
