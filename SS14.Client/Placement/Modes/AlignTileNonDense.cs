using SS14.Shared.Map;

namespace SS14.Client.Placement.Modes
{
    public class AlignTileNonDense : PlacementMode
    {
        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public AlignTileNonDense(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToPlayerGrid(mouseScreen);

            CurrentTile = pManager.MapManager.GetGrid(MouseCoords.GridId).GetTile(MouseCoords);
            float tileSize = pManager.MapManager.GetGrid(MouseCoords.GridId).TileSize; //convert from ushort to float
            GridDistancing = tileSize;

            if (pManager.CurrentPermission.IsTile)
            {
                MouseCoords = new GridCoordinates(CurrentTile.GridIndices.X + tileSize / 2,
                                                 CurrentTile.GridIndices.Y + tileSize / 2,
                                                 MouseCoords.GridId);
            }
            else
            {
                MouseCoords = new GridCoordinates(CurrentTile.GridIndices.X + tileSize / 2 + pManager.PlacementOffset.X,
                                                  CurrentTile.GridIndices.Y + tileSize / 2 + pManager.PlacementOffset.Y,
                                                  MouseCoords.GridId);
            }
        }

        public override bool IsValidPosition(GridCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }
            if (!pManager.CurrentPermission.IsTile && IsColliding(position))
            {
                return false;
            }

            return true;
        }
    }
}
