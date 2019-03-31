using System;
using JetBrains.Annotations;

namespace SS14.Shared.Map
{
    /// <summary>
    ///     This structure contains the data for an individual Tile in a <c>MapGrid</c>.
    /// </summary>
    [Serializable, PublicAPI]
    public readonly struct Tile : IEquatable<Tile>
    {
        /// <summary>
        ///     Internal type ID of this tile.
        /// </summary>
        public readonly ushort TileTypeId;

        /// <summary>
        ///     Optional per-tile data of this tile.
        /// </summary>
        public readonly ushort Data;

        /// <summary>
        ///     Is this tile space (empty)?
        /// </summary>
        public bool IsEmpty => TileTypeId == 0;

        /// <summary>
        ///     Creates a new instance of a grid tile.
        /// </summary>
        /// <param name="tileTypeId">Internal type ID.</param>
        /// <param name="data">Optional per-tile data.</param>
        public Tile(ushort tileTypeId, ushort data = 0)
        {
            TileTypeId = tileTypeId;
            Data = data;
        }

        /// <summary>
        ///     Explicit conversion of <c>Tile</c> to <c>uint</c> . This should only
        ///     be used in special cases like serialization. Do NOT use this in
        ///     content.
        /// </summary>
        public static explicit operator uint(Tile tile)
        {
            return ((uint) tile.TileTypeId << 16) | tile.Data;
        }

        /// <summary>
        ///     Explicit conversion of <c>uint</c> to <c>Tile</c> . This should only
        ///     be used in special cases like serialization. Do NOT use this in
        ///     content.
        /// </summary>
        public static explicit operator Tile(uint tile)
        {
            return new Tile(
                (ushort) (tile >> 16),
                (ushort) tile
            );
        }

        /// <summary>
        ///     Generates String representation of this Tile.
        /// </summary>
        /// <returns>String representation of this Tile.</returns>
        public override string ToString()
        {
            return $"Tile {TileTypeId}, {Data}";
        }

        /// <inheritdoc />
        public bool Equals(Tile other)
        {
            return TileTypeId == other.TileTypeId
                   && Data == other.Data;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is Tile other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (TileTypeId.GetHashCode() * 397) ^ Data.GetHashCode();
            }
        }

        /// <summary>
        ///     Check for equality by value between two objects.
        /// </summary>
        public static bool operator ==(Tile a, Tile b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Check for inequality by value between two objects.
        /// </summary>
        public static bool operator !=(Tile a, Tile b)
        {
            return !a.Equals(b);
        }
    }
}
