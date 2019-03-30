using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace SS14.Client.Placement.Modes
{
    public class PlaceNearby : PlacementMode
    {
        public PlaceNearby(PlacementManager pMan) : base(pMan) { }

        public override bool RangeRequired => true;

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToPlayerGrid(mouseScreen);
            CurrentTile = IoCManager.Resolve<IMapManager>().GetGrid(MouseCoords.GridID).GetTile(MouseCoords);
        }

        public override bool IsValidPosition(GridCoordinates position)
        {
            if (pManager.CurrentPermission.IsTile)
            {
                return false;
            }
            else if (!RangeCheck(position))
            {
                return false;
            }

            return true;
        }
    }
}
