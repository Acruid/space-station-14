using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Utility;

namespace SS14.Shared.Map
{
    /// <inheritdoc cref="IMapManager" />
    public partial class MapManager : IMapManager, IPostInjectInit
    {
        [Dependency]
        private readonly IGameTiming _gameTiming;

        /// <inheritdoc />
        public IMap DefaultMap => GetMap(MapId.Nullspace);

        /// <inheritdoc />
        public event EventHandler<TileChangedEventArgs> TileChanged;

        /// <inheritdoc />
        public event GridEventHandler OnGridCreated;

        /// <inheritdoc />
        public event GridEventHandler OnGridRemoved;

        /// <summary>
        ///     Should the OnTileChanged event be suppressed? This is useful for
        ///     initially loading the map so that you don't spam an event for
        ///     each of the million station tiles.
        /// </summary>
        /// <inheritdoc />
        public event EventHandler<GridChangedEventArgs> GridChanged;

        /// <inheritdoc />
        public event EventHandler<MapEventArgs> MapCreated;

        /// <inheritdoc />
        public event EventHandler<MapEventArgs> MapDestroyed;

        /// <inheritdoc />
        public bool SuppressOnTileChanged { get; set; }

        private MapId HighestMapID = MapId.Nullspace;
        private GridId HighestGridID = GridId.Nullspace;

        /// <summary>
        ///     Holds an indexed collection of map grids.
        /// </summary>
        private readonly Dictionary<MapId, Map> _maps = new Dictionary<MapId, Map>();

        private readonly Dictionary<GridId, MapGrid> _grids = new Dictionary<GridId, MapGrid>();

        private readonly List<(uint tick, GridId gridId)> _gridDeletionHistory = new List<(uint, GridId)>();
        private readonly List<(uint tick, MapId mapId)> _mapDeletionHistory = new List<(uint, MapId)>();

        /// <inheritdoc />
        public void PostInject()
        {
            CreateMap(MapId.Nullspace, GridId.Nullspace);
        }

        /// <inheritdoc />
        public void Initialize()
        {
            // So uh I removed the contents from this but I'm too lazy to remove the Initialize method.
            // Deal with it.
        }

        /// <inheritdoc />
        public void Startup()
        {
            // Ditto, removed contents but too lazy to remove method.
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            foreach (var map in _maps.Keys.ToArray())
            {
                if (map != MapId.Nullspace)
                {
                    DeleteMap(map);
                }
            }

            DebugTools.Assert(_grids.Count == 1);
        }

        /// <summary>
        ///     Raises the OnTileChanged event.
        /// </summary>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile that got replaced.</param>
        private void RaiseOnTileChanged(in TileRef tileRef, Tile oldTile)
        {
            if (SuppressOnTileChanged)
                return;

            TileChanged?.Invoke(this, new TileChangedEventArgs(tileRef, oldTile));
        }


        /// <inheritdoc />
        public void DeleteMap(MapId mapId)
        {
            if (!_maps.TryGetValue(mapId, out var map))
            {
                throw new InvalidOperationException($"Attempted to delete nonexistent map '{mapId}'");
            }

            // grids are cached because Delete modifies collection
            foreach (var grid in map.GetAllGrids().ToList())
            {
                DeleteGrid(grid.Index);
            }

            MapDestroyed?.Invoke(this, new MapEventArgs(_maps[mapId]));
            _maps.Remove(mapId);

            if (_netManager.IsClient)
                return;

            _mapDeletionHistory.Add((tick: _gameTiming.CurTick, mapId));
        }

        /// <inheritdoc />
        public IMap CreateMap(MapId? mapId = null, GridId? defaultGridId = null)
        {
            if (defaultGridId != null && GridExists(defaultGridId.Value))
            {
                throw new InvalidOperationException($"Grid '{defaultGridId}' already exists.");
            }

            var actualId = mapId ?? new MapId(HighestMapID.Value + 1);

            if (MapExists(actualId))
            {
                throw new InvalidOperationException($"A map with ID {actualId} already exists");
            }

            if (HighestMapID.Value < actualId.Value)
            {
                HighestMapID = actualId;
            }

            var newMap = new Map(this, actualId);
            _maps.Add(actualId, newMap);
            MapCreated?.Invoke(this, new MapEventArgs(newMap));
            newMap.DefaultGrid = CreateGrid(newMap.Index, defaultGridId);

            return newMap;
        }

        /// <inheritdoc />
        public IMap GetMap(MapId mapId)
        {
            return _maps[mapId];
        }

        /// <inheritdoc />
        public bool MapExists(MapId mapId)
        {
            return _maps.ContainsKey(mapId);
        }

        /// <inheritdoc />
        public bool TryGetMap(MapId mapId, out IMap map)
        {
            if (_maps.TryGetValue(mapId, out var mapInterface))
            {
                map = mapInterface;
                return true;
            }
            map = null;
            return false;
        }

        /// <inheritdoc />
        public IEnumerable<IMap> GetAllMaps()
        {
            return _maps.Values;
        }

        /// <inheritdoc />
        public IMapGrid CreateGrid(MapId currentMapId, GridId? gridId = null, ushort chunkSize = 16, float snapSize = 1)
        {
            var map = _maps[currentMapId];

            var actualId = gridId ?? new GridId(HighestGridID.Value + 1);

            if (GridExists(actualId))
            {
                throw new InvalidOperationException($"A map with ID {actualId} already exists");
            }

            if (HighestGridID.Value < actualId.Value)
            {
                HighestGridID = actualId;
            }

            var grid = new MapGrid(this, actualId, chunkSize, snapSize, currentMapId);
            _grids.Add(actualId, grid);
            map.AddGrid(grid);
            OnGridCreated?.Invoke(actualId);
            return grid;
        }

        /// <inheritdoc />
        public IMapGrid GetGrid(GridId gridId)
        {
            return _grids[gridId];
        }

        /// <inheritdoc />
        public bool TryGetGrid(GridId gridId, out IMapGrid grid)
        {
            if (_grids.TryGetValue(gridId, out var gridInterface))
            {
                grid = gridInterface;
                return true;
            }
            grid = null;
            return false;
        }

        /// <inheritdoc />
        public bool GridExists(GridId gridId)
        {
            return _grids.ContainsKey(gridId);
        }

        /// <inheritdoc />
        public void DeleteGrid(GridId gridId)
        {
            var grid = _grids[gridId];
            var map = (Map)grid.ParentMap;

            grid.Dispose();
            map.RemoveGrid(grid);
            _grids.Remove(grid.Index);

            OnGridRemoved?.Invoke(gridId);

            if (_netManager.IsClient)
            {
                return;
            }
            _gridDeletionHistory.Add((_gameTiming.CurTick, gridId));
        }
    }
    
    /// <summary>
    ///     Arguments for when a map is created or deleted locally ore remotely.
    /// </summary>
    public class MapEventArgs : EventArgs
    {
        /// <summary>
        ///     Map that is being modified.
        /// </summary>
        public IMap Map { get; }
        
        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public MapEventArgs(IMap map)
        {
            Map = map;
        }
    }

    /// <summary>
    ///     Arguments for when a single tile on a grid is changed locally or remotely.
    /// </summary>
    public class TileChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     New tile that replaced the old one.
        /// </summary>
        public TileRef NewTile { get; }

        /// <summary>
        ///     Old tile that was replaced.
        /// </summary>
        [PublicAPI]
        public Tile OldTile { get; }

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public TileChangedEventArgs(TileRef newTile, Tile oldTile)
        {
            NewTile = newTile;
            OldTile = oldTile;
        }
    }

    /// <summary>
    ///     Arguments for when a one or more tiles on a grid is changed at once.
    /// </summary>
    public class GridChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     Grid being changed.
        /// </summary>
        public IMapGrid Grid { get; }

        /// <summary>
        ///     Tiles that were changed in this batch.
        /// </summary>
        public IReadOnlyCollection<(MapIndices position, Tile tile)> Modified { get; }

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public GridChangedEventArgs(IMapGrid grid, IReadOnlyCollection<(MapIndices position, Tile tile)> modified)
        {
            Grid = grid;
            Modified = modified;
        }
    }
}
