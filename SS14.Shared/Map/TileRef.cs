using System;
using JetBrains.Annotations;

namespace SS14.Shared.Map
{
    /// <summary>
    ///     All of the information needed to reference a tile in the game.
    /// </summary>
    [PublicAPI]
    public readonly struct TileRef : IEquatable<TileRef>
    {
        /// <summary>
        ///     Identifier of the map this tile belongs to.
        /// </summary>
        public readonly MapId MapId;

        /// <summary>
        ///     Identifier of the grid this tile belongs to.
        /// </summary>
        public readonly GridId GridId;

        /// <summary>
        ///     Actual data of this tile.
        /// </summary>
        public readonly Tile Tile;

        /// <summary>
        ///     Positional indices of this tile on the grid.
        /// </summary>
        public readonly MapIndices GridIndices;

        /// <summary>
        ///     Constructs a new instance of TileRef.
        /// </summary>
        /// <param name="mapId">Identifier of the map this tile belongs to.</param>
        /// <param name="gridId">Identifier of the grid this tile belongs to.</param>
        /// <param name="xIndex">Positional X index of this tile on the grid.</param>
        /// <param name="yIndex">Positional Y index of this tile on the grid.</param>
        /// <param name="tile">Actual data of this tile.</param>
        internal TileRef(MapId mapId, GridId gridId, int xIndex, int yIndex, Tile tile)
            : this(mapId, gridId, new MapIndices(xIndex, yIndex), tile) { }

        /// <summary>
        ///     Constructs a new instance of TileRef.
        /// </summary>
        /// <param name="mapId">Identifier of the map this tile belongs to.</param>
        /// <param name="gridId">Identifier of the grid this tile belongs to.</param>
        /// <param name="gridIndices">Positional indices of this tile on the grid.</param>
        /// <param name="tile">Actual data of this tile.</param>
        internal TileRef(MapId mapId, GridId gridId, MapIndices gridIndices, Tile tile)
        {
            MapId = mapId;
            GridIndices = gridIndices;
            GridId = gridId;
            Tile = tile;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"TileRef: {GridId}:{GridIndices} ({Tile})";
        }

        /// <inheritdoc />
        public bool Equals(TileRef other)
        {
            return MapId.Equals(other.MapId)
                   && GridId.Equals(other.GridId)
                   && Tile.Equals(other.Tile)
                   && GridIndices.Equals(other.GridIndices);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is TileRef other && Equals(other);
        }

        /// <summary>
        ///     Check for equality by value between two objects.
        /// </summary>
        public static bool operator ==(TileRef a, TileRef b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Check for inequality by value between two objects.
        /// </summary>
        public static bool operator !=(TileRef a, TileRef b)
        {
            return !a.Equals(b);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = MapId.GetHashCode();
                hashCode = (hashCode * 397) ^ GridId.GetHashCode();
                hashCode = (hashCode * 397) ^ Tile.GetHashCode();
                hashCode = (hashCode * 397) ^ GridIndices.GetHashCode();
                return hashCode;
            }
        }
    }
}
