using System;
using System.Diagnostics.CodeAnalysis;
using SS14.Shared.Serialization;

namespace SS14.Shared.Map
{
    /// <summary>
    /// Represents a grid index on a map.
    /// </summary>
    [Serializable, NetSerializable]
    public readonly struct GridId : IEquatable<GridId>
    {
        public static readonly GridId Nullspace = new GridId(0);

        internal readonly int Value;

        public GridId(int value)
        {
            Value = value;
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public bool Equals(GridId other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GridId id && Equals(id);
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            return Value;
        }

        [ExcludeFromCodeCoverage]
        public static bool operator ==(GridId a, GridId b)
        {
            return a.Value == b.Value;
        }

        [ExcludeFromCodeCoverage]
        public static bool operator !=(GridId a, GridId b)
        {
            return !(a == b);
        }

        [ExcludeFromCodeCoverage]
        public static explicit operator int(GridId self)
        {
            return self.Value;
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
