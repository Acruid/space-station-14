using System;
using System.Diagnostics.CodeAnalysis;

namespace SS14.Shared.Map
{
    /// <summary>
    ///     The tile data of a cell in a <c>MapGrid</c>.
    /// </summary>
    [Serializable]
    public readonly struct Tile : IEquatable<Tile>
    {
        /// <summary>
        ///     Internal type ID of this tile.
        /// </summary>
        public readonly ushort TileId;

        /// <summary>
        ///     Optional per-tile data of this tile.
        /// </summary>
        public readonly ushort Data;

        /// <summary>
        ///     Is this tile space (empty)?
        /// </summary>
        public bool IsEmpty => TileId == 0;

        /// <summary>
        ///     Creates a new instance of a grid tile.
        /// </summary>
        /// <param name="tileId">Internal type ID.</param>
        /// <param name="data">Optional per-tile data.</param>
        public Tile(ushort tileId, ushort data = 0)
        {
            TileId = tileId;
            Data = data;
        }

        /// <summary>
        ///     Explicitly casts tile data to an unsigned integer.
        ///     This should only be used for serialization, not in normal code.
        /// </summary>
        /// <param name="tile">Tile data to cast.</param>
        public static explicit operator uint(Tile tile)
        {
            return ((uint) tile.TileId << 16) | tile.Data;
        }

        /// <summary>
        ///     Explicitly casts an unsigned integer to tile data.
        ///     This should only be used for serialization, not in normal code.
        /// </summary>
        /// <param name="tile"></param>
        public static explicit operator Tile(uint tile)
        {
            return new Tile(
                (ushort) (tile >> 16),
                (ushort) tile
            );
        }

        #region IEquatable

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public bool Equals(Tile other)
        {
            return TileId == other.TileId && Data == other.Data;
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is Tile other && Equals(other);
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            unchecked
            {
                return (TileId.GetHashCode() * 397) ^ Data.GetHashCode();
            }
        }

        /// <summary>
        ///     Checks for the equality between two <c>Tile</c>s.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public static bool operator ==(Tile a, Tile b)
        {
            return a.TileId == b.TileId && a.Data == b.Data;
        }

        /// <summary>
        ///     Checks for the inequality between two <c>Tile</c>s.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public static bool operator !=(Tile a, Tile b)
        {
            return !(a == b);
        }

        #endregion

        /// <summary>
        ///     Generates String representation of this Tile.
        /// </summary>
        /// <returns>String representation of this Tile.</returns>
        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return $"Tile {TileId}, {Data}";
        }
    }
}
