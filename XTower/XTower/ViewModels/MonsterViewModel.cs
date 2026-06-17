namespace XTower.ViewModels
{
    internal partial class MonsterViewModel : DockViewModel
    {
        public override string Id => "monster";

        public override string Header => "怪物";

        public override DockPosition DockPosition => DockPosition.Right;
    }
}
