using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace SS14.Client.Placement.Modes
{
    public class AlignTileDense : PlacementMode
    {
        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public AlignTileDense(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToPlayerGrid(mouseScreen);

            CurrentTile = IoCManager.Resolve<IMapManager>().GetGrid(MouseCoords.GridID).GetTile(MouseCoords);
            float tileSize = IoCManager.Resolve<IMapManager>().GetGrid(MouseCoords.GridID).TileSize; //convert from ushort to float
            GridDistancing = tileSize;

            if (pManager.CurrentPermission.IsTile)
            {
                MouseCoords = new GridCoordinates(CurrentTile.GridIndices.X + tileSize / 2,
                                                  CurrentTile.GridIndices.Y + tileSize / 2,
                                                  IoCManager.Resolve<IMapManager>().GetGrid(MouseCoords.GridID));
            }
            else
            {
                MouseCoords = new GridCoordinates(CurrentTile.GridIndices.X + tileSize / 2 + pManager.PlacementOffset.X,
                                                  CurrentTile.GridIndices.Y + tileSize / 2 + pManager.PlacementOffset.Y,
                                                  IoCManager.Resolve<IMapManager>().GetGrid(MouseCoords.GridID));
            }
        }

        public override bool IsValidPosition(GridCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }
            if (!pManager.CurrentPermission.IsTile && !IsColliding(position))
            {
                return false;
            }

            return true;
        }
    }
}
