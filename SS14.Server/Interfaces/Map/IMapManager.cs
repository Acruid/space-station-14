using Lidgren.Network;
using SFML.System;
using SS14.Shared;
using System.Collections.Generic;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Server.Interfaces.Map
{
    public delegate void TileChangedEventHandler(TileRef tileRef, Tile oldTile);

    public interface IMapManager
    {
        bool LoadMap(string mapName);
        void SaveMap(string mapName);

        event TileChangedEventHandler TileChanged;

        void HandleNetworkMessage(NetIncomingMessage message);
        NetOutgoingMessage CreateMapMessage(MapMessage messageType);
        void SendMap(NetConnection connection);

        int TileSize { get; }

        IEnumerable<TileRef> GetTilesIntersecting(RectF area, bool ignoreSpace);
        IEnumerable<TileRef> GetGasTilesIntersecting(RectF area);
        IEnumerable<TileRef> GetWallsIntersecting(RectF area);
        IEnumerable<TileRef> GetAllTiles();

        TileRef GetTileRef(Vector2f pos);
        TileRef GetTileRef(int x, int y);
        ITileCollection Tiles { get; }
    }
}
