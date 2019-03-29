using System;
using System.Diagnostics.CodeAnalysis;

namespace SS14.Shared.Map
{
    /// <summary>
    /// A reference to a tile.
    /// </summary>
    public readonly struct TileRef : IEquatable<TileRef>
    {
        /// <summary>
        /// Map index that the tile is on.
        /// </summary>
        public readonly MapId MapIndex;

        /// <summary>
        /// Grid index that the tile is on.
        /// </summary>
        public readonly GridId GridIndex;

        /// <summary>
        /// Location indices of the tile on the grid.
        /// </summary>
        public readonly MapIndices GridIndices;

        /// <summary>
        /// Data of the tile.
        /// </summary>
        public readonly Tile Tile;

        /// <summary>
        ///     Constructs a new instance of <see cref="TileRef"/>.
        /// </summary>
        /// <param name="mapIndex">Map index that the tile is on.</param>
        /// <param name="gridIndex">Grid index that the tile is on.</param>
        /// <param name="xIndex">X location index of the tile on the grid.</param>
        /// <param name="yIndex">Y location index of the tile on the grid.</param>
        /// <param name="tile">Data of this tile.</param>
        internal TileRef(MapId mapIndex, GridId gridIndex, int xIndex, int yIndex, Tile tile)
            : this(mapIndex, gridIndex, new MapIndices(xIndex, yIndex), tile) { }

        /// <summary>
        ///     Constructs a new instance of <see cref="TileRef"/>.
        /// </summary>
        /// <param name="mapIndex">Map index that the tile is on.</param>
        /// <param name="gridIndex">Grid index that the tile is on.</param>
        /// <param name="gridIndices">Location indices of the tile on the grid.</param>
        /// <param name="tile">Data of this tile.</param>
        internal TileRef(MapId mapIndex, GridId gridIndex, MapIndices gridIndices, Tile tile)
        {
            MapIndex = mapIndex;
            GridIndices = gridIndices;
            GridIndex = gridIndex;
            Tile = tile;
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return $"map={MapIndex},grid={GridIndex},indices={GridIndices},tile={Tile}";
        }

        #region IEquatable

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public bool Equals(TileRef other)
        {
            return MapIndex.Equals(other.MapIndex)
                   && GridIndex.Equals(other.GridIndex)
                   && GridIndices.Equals(other.GridIndices)
                   && Tile.Equals(other.Tile);
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is TileRef other && Equals(other);
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = MapIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ GridIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ GridIndices.GetHashCode();
                hashCode = (hashCode * 397) ^ Tile.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        ///     Checks for the equality between two <c>Tile</c>s.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public static bool operator ==(TileRef a, TileRef b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Checks for the inequality between two <c>Tile</c>s.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public static bool operator !=(TileRef a, TileRef b)
        {
            return !a.Equals(b);
        }

        #endregion
    }
}
