using System;
using System.Diagnostics.CodeAnalysis;
using SS14.Shared.Serialization;

namespace SS14.Shared.Map
{
    [Serializable, NetSerializable]
    public readonly struct MapId : IEquatable<MapId>
    {
        public static readonly MapId Nullspace = new MapId(0);

        internal readonly int Value;

        public MapId(int value)
        {
            Value = value;
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public bool Equals(MapId other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MapId id && Equals(id);
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            return Value;
        }

        [ExcludeFromCodeCoverage]
        public static bool operator ==(MapId a, MapId b)
        {
            return a.Value == b.Value;
        }

        [ExcludeFromCodeCoverage]
        public static bool operator !=(MapId a, MapId b)
        {
            return !(a == b);
        }

        [ExcludeFromCodeCoverage]
        public static explicit operator int(MapId self)
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
