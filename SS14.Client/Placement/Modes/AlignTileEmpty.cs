using SS14.Shared.IoC;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Placement.Modes
{
    public class AlignTileEmpty : PlacementMode
    {
        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public AlignTileEmpty(PlacementManager pMan) : base(pMan) { }

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

            var entitymanager = IoCManager.Resolve<IClientEntityManager>();
            return !(entitymanager.AnyEntitiesIntersecting(pManager.MapManager.GetGrid(MouseCoords.GridId).Map.Index,
                new Box2(new Vector2(CurrentTile.GridIndices.X, CurrentTile.GridIndices.Y), new Vector2(CurrentTile.GridIndices.X + 0.99f, CurrentTile.GridIndices.Y + 0.99f))));
        }
    }
}
