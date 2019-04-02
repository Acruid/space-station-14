using SS14.Shared.Interfaces.Map;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
using System;
using JetBrains.Annotations;

namespace SS14.Shared.Map
{
    /// <summary>
    ///     Coordinates relative to a specific grid.
    /// </summary>
    [Serializable, NetSerializable]
    public readonly struct GridCoordinates : IEquatable<GridCoordinates>
    {
        /// <summary>
        ///     Map grid that this position is relative to.
        /// </summary>
        public readonly GridId GridId;

        /// <summary>
        ///     Local Position coordinates.
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        ///     A set of coordinates that is at the origin of nullspace.
        ///     This is also the values of an uninitialized struct.
        /// </summary>
        public static readonly GridCoordinates Nullspace = new GridCoordinates(0, 0, GridId.Nullspace);

        public GridCoordinates(Vector2 position, GridId gridId)
        {
            Position = position;
            GridId = gridId;
        }

        /// <summary>
        ///     Construct new grid local coordinates relative to the default grid of a map.
        /// </summary>
        public GridCoordinates(Vector2 position, IMap map)
            : this(position, map.DefaultGrid.Index) { }

        public GridCoordinates(float x, float y, GridId gridId)
            : this(new Vector2(x, y), gridId) { }

        public GridCoordinates ConvertToGrid(IMapManager mapManager, IMapGrid argGrid)
        {
            return new GridCoordinates(Position + mapManager.GetGrid(GridId).WorldPosition - argGrid.WorldPosition, argGrid.Index);
        }

        public GridCoordinates ToWorld(IMapManager mapManager)
        {
            return ConvertToGrid(mapManager, mapManager.GetGrid(GridId).Map.DefaultGrid);
        }

        public GridCoordinates Offset(Vector2 offset)
        {
            return new GridCoordinates(Position + offset, GridId);
        }

        public bool InRange(IMapManager mapManager, GridCoordinates localpos, float range)
        {
            if (mapManager.GetGrid(localpos.GridId).Map.Index != mapManager.GetGrid(GridId).Map.Index)
            {
                return false;
            }

            return ((localpos.ToWorld(mapManager).Position - ToWorld(mapManager).Position).LengthSquared < range * range);
        }

        public bool InRange(IMapManager mapManager, GridCoordinates localpos, int range)
        {
            return InRange(mapManager, localpos, (float) range);
        }

        public GridCoordinates Translated(Vector2 offset)
        {
            return new GridCoordinates(Position + offset, GridId);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Grid={GridId}, X={Position.X:N2}, Y={Position.Y:N2}";
        }

        /// <inheritdoc />
        public bool Equals(GridCoordinates other)
        {
            return GridId.Equals(other.GridId) && Position.Equals(other.Position);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GridCoordinates && Equals((GridCoordinates) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = GridId.GetHashCode();
                hashCode = (hashCode * 397) ^ Position.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        ///     Tests for value equality between two LocalCoordinates.
        /// </summary>
        public static bool operator ==(GridCoordinates self, GridCoordinates other)
        {
            return self.Equals(other);
        }

        /// <summary>
        ///     Tests for value inequality between two LocalCoordinates.
        /// </summary>
        public static bool operator !=(GridCoordinates self, GridCoordinates other)
        {
            return !self.Equals(other);
        }
    }

    /// <summary>
    ///     Contains the coordinates of a position on the rendering screen.
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    public readonly struct ScreenCoordinates : IEquatable<ScreenCoordinates>
    {
        /// <summary>
        ///     Position on the rendering screen.
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        ///     Constructs a new instance of <c>ScreenCoordinates</c>.
        /// </summary>
        /// <param name="position">Position on the rendering screen.</param>
        public ScreenCoordinates(Vector2 position)
        {
            Position = position;
        }

        /// <summary>
        ///     Constructs a new instance of <c>ScreenCoordinates</c>.
        /// </summary>
        /// <param name="x">X axis of a position on the screen.</param>
        /// <param name="y">Y axis of a position on the screen.</param>
        public ScreenCoordinates(float x, float y)
        {
            Position = new Vector2(x, y);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Position.ToString();
        }

        /// <inheritdoc />
        public bool Equals(ScreenCoordinates other)
        {
            return Position.Equals(other.Position);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is ScreenCoordinates other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        /// <summary>
        ///     Check for equality by value between two objects.
        /// </summary>
        public static bool operator ==(ScreenCoordinates a, ScreenCoordinates b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Check for inequality by value between two objects.
        /// </summary>
        public static bool operator !=(ScreenCoordinates a, ScreenCoordinates b)
        {
            return !a.Equals(b);
        }
    }

    /// <summary>
    ///     Coordinates relative to a specific map.
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    public readonly struct MapCoordinates : IEquatable<MapCoordinates>
    {
        /// <summary>
        ///     World Position coordinates.
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        ///     Map identifier relevant to this position.
        /// </summary>
        public readonly MapId MapId;

        /// <summary>
        ///     Constructs a new instance of <c>MapCoordinates</c>.
        /// </summary>
        /// <param name="position">World position coordinates.</param>
        /// <param name="mapId">Map identifier relevant to this position.</param>
        public MapCoordinates(Vector2 position, MapId mapId)
        {
            Position = position;
            MapId = mapId;
        }

        /// <summary>
        ///     Constructs a new instance of <c>MapCoordinates</c>.
        /// </summary>
        /// <param name="x">World position coordinate on the X axis.</param>
        /// <param name="y">World position coordinate on the Y axis.</param>
        /// <param name="mapId">Map identifier relevant to this position.</param>
        public MapCoordinates(float x, float y, MapId mapId)
            : this(new Vector2(x, y), mapId) { }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({Position.X}, {Position.Y}, map: {MapId})";
        }

        /// <inheritdoc />
        public bool Equals(MapCoordinates other)
        {
            return Position.Equals(other.Position) && MapId.Equals(other.MapId);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is MapCoordinates other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ MapId.GetHashCode();
            }
        }

        /// <summary>
        ///     Check for equality by value between two objects.
        /// </summary>
        public static bool operator ==(MapCoordinates a, MapCoordinates b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Check for inequality by value between two objects.
        /// </summary>
        public static bool operator !=(MapCoordinates a, MapCoordinates b)
        {
            return !a.Equals(b);
        }
    }
}
