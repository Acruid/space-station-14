using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace SS14.Client.Placement.Modes
{
    public class PlaceFree : PlacementMode
    {
        public PlaceFree(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToPlayerGrid(mouseScreen);
            CurrentTile = IoCManager.Resolve<IMapManager>().GetGrid(MouseCoords.GridId).GetTile(MouseCoords);
        }

        public override bool IsValidPosition(GridCoordinates position)
        {
            return true;
        }
    }
}
