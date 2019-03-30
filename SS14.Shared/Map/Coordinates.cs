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
        public readonly GridId GridID;
        public readonly Vector2 Position;

        public static readonly GridCoordinates Nullspace = new GridCoordinates(0, 0, GridId.Nullspace);

        public GridCoordinates(Vector2 argPosition, IMapGrid argGrid)
        {
            Position = argPosition;
            GridID = argGrid.Index;
        }

        public GridCoordinates(Vector2 argPosition, GridId argGrid)
        {
            Position = argPosition;
            GridID = argGrid;
        }

        /// <summary>
        ///     Construct new grid local coordinates relative to the default grid of a map.
        /// </summary>
        public GridCoordinates(Vector2 argPosition, IMap argMap)
        {
            Position = argPosition;
            GridID = argMap.DefaultGrid.Index;
        }

        public GridCoordinates(float x, float y, IMapGrid argGrid)
        {
            Position = new Vector2(x, y);
            GridID = argGrid.Index;
        }

        public GridCoordinates(float x, float y, GridId argGrid)
        {
            Position = new Vector2(x, y);
            GridID = argGrid;
        }

        /// <summary>
        ///     Construct new grid local coordinates relative to the default grid of a map.
        /// </summary>
        public GridCoordinates(float x, float y, IMap argMap) : this(new Vector2(x, y), argMap)
        {
        }

        public bool IsValidLocation(IMapManager mapManager)
        {
            return mapManager.GridExists(GridID);
        }

        public GridCoordinates ConvertToGrid(IMapManager mapManager, IMapGrid newGrid)
        {
            return new GridCoordinates(Position + mapManager.GetGrid(GridID).WorldPosition - newGrid.WorldPosition, newGrid);
        }

        public GridCoordinates ToWorld(IMapManager mapManager, IMapGrid grid)
        {
            return ConvertToGrid(mapManager, grid.ParentMap.DefaultGrid);
        }

        public GridCoordinates Offset(Vector2 offset)
        {
            return new GridCoordinates(Position + offset, GridID);
        }

        public bool InRange(IMapManager mapManager, GridCoordinates localPos, float range)
        {
            if (mapManager.GetGrid(localPos.GridID).ParentMap.Index != mapManager.GetGrid(GridID).ParentMap.Index)
            {
                return false;
            }

            return ((localPos.ToWorld(mapManager, mapManager.GetGrid(localPos.GridID)).Position - ToWorld(mapManager, mapManager.GetGrid(GridID)).Position).LengthSquared < range * range);
        }

        public bool InRange(IMapManager mapManager, GridCoordinates localPos, int range)
        {
            return InRange(mapManager, localPos, (float) range);
        }

        public GridCoordinates Translated(Vector2 offset)
        {
            return new GridCoordinates(Position + offset, GridID);
        }

        public override string ToString()
        {
            return $"Grid={GridID}, X={Position.X:N2}, Y={Position.Y:N2}";
        }

        public bool Equals(GridCoordinates other)
        {
            return GridID.Equals(other.GridID) && Position.Equals(other.Position);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GridCoordinates && Equals((GridCoordinates) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = GridID.GetHashCode();
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
            return !(self == other);
        }
    }

    [Serializable, NetSerializable]
    public readonly struct ScreenCoordinates
    {
        public readonly Vector2 Position;

        public ScreenCoordinates(Vector2 argPosition)
        {
            Position = argPosition;
        }

        public ScreenCoordinates(float x, float y)
        {
            Position = new Vector2(x, y);
        }

        public override string ToString()
        {
            return Position.ToString();
        }
    }

    /// <summary>
    ///     Coordinates relative to a specific map.
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    public readonly struct MapCoordinates
    {
        public readonly Vector2 Position;
        public readonly MapId MapId;

        public MapCoordinates(Vector2 position, MapId mapId)
        {
            Position = position;
            MapId = mapId;
        }

        public MapCoordinates(float x, float y, MapId mapId)
            : this(new Vector2(x, y), mapId) { }

        public override string ToString()
        {
            return $"({Position.X}, {Position.Y}, map: {MapId})";
        }
    }
}
