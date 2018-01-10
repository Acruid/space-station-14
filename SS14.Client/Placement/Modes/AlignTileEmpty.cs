﻿using OpenTK;
using SS14.Client.Graphics;
using SS14.Shared.IoC;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Placement.Modes
{
    public class AlignTileEmpty : PlacementMode
    {
        public AlignTileEmpty(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapId.Nullspace) return false;

            MouseScreen = mouseS;
            MouseCoords = CluwneLib.ScreenToCoordinates(MouseScreen);

            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);
            var tilesize = MouseCoords.Grid.TileSize;

            if (!RangeCheck())
                return false;

            var entitymanager = IoCManager.Resolve<IClientEntityManager>();
            var failtoplace = !entitymanager.AnyEntitiesIntersecting(new Box2(new Vector2(CurrentTile.X, CurrentTile.Y), new Vector2(CurrentTile.X + 0.99f, CurrentTile.Y + 0.99f)));

            if (pManager.CurrentPermission.IsTile)
            {
                MouseCoords = new LocalCoordinates(CurrentTile.X + tilesize/2,
                                                  CurrentTile.Y + tilesize/2,
                                                  MouseCoords.Grid);
                MouseScreen = CluwneLib.WorldToScreen(MouseCoords);
            }
            else
            {
                MouseCoords = new LocalCoordinates(CurrentTile.X + tilesize/2 + pManager.CurrentPrototype.PlacementOffset.X,
                                                  CurrentTile.Y + tilesize/2 + pManager.CurrentPrototype.PlacementOffset.Y,
                                                  MouseCoords.Grid);
                MouseScreen = CluwneLib.WorldToScreen(MouseCoords);
            }
            return failtoplace;
        }
    }
}
