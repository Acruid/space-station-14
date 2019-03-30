using System;
using System.Collections.Generic;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Maths;

namespace SS14.Shared.Map
{
    public partial class MapManager
    {
        /// <inheritdoc />
        public class MapGrid : IMapGrid
        {
            /// <summary>
            ///     Game tick that the map was created.
            /// </summary>
            public uint CreatedTick { get; }

            /// <summary>
            ///     Last game tick that the map was modified.
            /// </summary>
            public uint LastModifiedTick { get; internal set; }

            /// <inheritdoc />
            public bool IsDefaultGrid => ParentMap.DefaultGrid == this;

            /// <inheritdoc />
            public IMap ParentMap => _mapManager.GetMap(MapId);

            /// <inheritdoc />
            public MapId MapId { get; }

            /// <summary>
            ///     Grid chunks than make up this grid.
            /// </summary>
            internal readonly Dictionary<MapIndices, Chunk> Chunks = new Dictionary<MapIndices, Chunk>();

            private readonly MapManager _mapManager;
            private Vector2 _worldPosition;

            /// <summary>
            ///     Initializes a new instance of the <see cref="MapGrid"/> class.
            /// </summary>
            /// <param name="mapManager">Reference to the <see cref="MapManager"/> that will manage this grid.</param>
            /// <param name="gridIndex">Index identifier of this grid.</param>
            /// <param name="chunkSize">The dimension of this square chunk.</param>
            /// <param name="snapSize"></param>
            /// <param name="mapId"></param>
            internal MapGrid(MapManager mapManager, GridId gridIndex, ushort chunkSize, float snapSize, MapId mapId)
            {
                _mapManager = mapManager;
                Index = gridIndex;
                ChunkSize = chunkSize;
                SnapSize = snapSize;
                MapId = mapId;
                LastModifiedTick = CreatedTick = _mapManager._gameTiming.CurTick;
            }

            /// <summary>
            ///     Disposes the grid.
            /// </summary>
            public void Dispose()
            {
                // Nothing for now.
            }

            /// <inheritdoc />
            public Box2 AABBWorld { get; private set; }

            /// <inheritdoc />
            public ushort ChunkSize { get; }

            /// <inheritdoc />
            public float SnapSize { get; }
            
            /// <inheritdoc />
            public GridId Index { get; }

            /// <inheritdoc />
            public ushort TileSize { get; } = 1;

            /// <inheritdoc />
            public Vector2 WorldPosition
            {
                get => _worldPosition;
                set
                {
                    _worldPosition = value;
                    LastModifiedTick = _mapManager._gameTiming.CurTick;
                }
            }

            /// <summary>
            /// Expands the AABB for this grid when a new tile is added. If the tile is already inside the existing AABB,
            /// nothing happens. If it is outside, the AABB is expanded to fit the new tile.
            /// </summary>
            /// <param name="gridTile">The new tile to check.</param>
            public void UpdateAABB(MapIndices gridTile)
            {
                GridCoordinates tempQualifier = GridTileToLocal(gridTile);
                var worldPos = GridTileToLocal(gridTile).ToWorld(_mapManager, _mapManager.GetGrid(tempQualifier.GridID));

                if (AABBWorld.Contains(worldPos.Position))
                    return;

                // rect union
                var a = AABBWorld;
                var b = worldPos;

                var minX = Math.Min(a.Left, b.Position.X);
                var maxX = Math.Max(a.Right, b.Position.X);

                var minY = Math.Min(a.Bottom, b.Position.Y);
                var maxY = Math.Max(a.Top, b.Position.Y);

                AABBWorld = Box2.FromDimensions(minX, minY, maxX - minX, maxY - minY);
            }

            /// <inheritdoc />
            public bool OnSnapCenter(Vector2 position)
            {
                return (FloatMath.CloseTo(position.X % SnapSize, 0) && FloatMath.CloseTo(position.Y % SnapSize, 0));
            }

            /// <inheritdoc />
            public bool OnSnapBorder(Vector2 position)
            {
                return (FloatMath.CloseTo(position.X % SnapSize, SnapSize / 2) && FloatMath.CloseTo(position.Y % SnapSize, SnapSize / 2));
            }

            #region TileAccess

            /// <inheritdoc />
            public TileRef GetTile(GridCoordinates worldPos)
            {
                var chunkIndices = WorldToChunk(worldPos);
                var gridTileIndices = WorldToTile(worldPos);

                if (!Chunks.TryGetValue(chunkIndices, out var output))
                    return new TileRef(MapId, Index, gridTileIndices.X, gridTileIndices.Y, default);

                var chunkTileIndices = output.GridTileToChunkTile(gridTileIndices);
                return output.GetTile((ushort)chunkTileIndices.X, (ushort)chunkTileIndices.Y);
            }

            /// <inheritdoc />
            public IEnumerable<TileRef> GetAllTiles(bool ignoreSpace = true)
            {
                foreach (var kvChunk in Chunks)
                {
                    foreach (var tileRef in kvChunk.Value)
                    {
                        if (!tileRef.Tile.IsEmpty)
                            yield return tileRef;
                    }
                }
            }

            /// <inheritdoc />
            public void SetTile(GridCoordinates worldPos, Tile tile)
            {
                var localTile = WorldToTile(worldPos);
                SetTile(localTile.X, localTile.Y, tile);
            }

            private void SetTile(int xIndex, int yIndex, Tile tile)
            {
                var (chunk, chunkTile) = ChunkAndOffsetForTile(new MapIndices(xIndex, yIndex));
                chunk.SetTile((ushort)chunkTile.X, (ushort)chunkTile.Y, tile);
            }

            /// <inheritdoc />
            public void SetTile(GridCoordinates worldPos, ushort tileId, ushort tileData = 0)
            {
                SetTile(worldPos, new Tile(tileId, tileData));
            }

            /// <inheritdoc />
            public IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true, Predicate<TileRef> predicate = null)
            {
                //TODO: needs world -> local -> tile translations.
                var gridTileLb = new MapIndices((int)Math.Floor(worldArea.Left), (int)Math.Floor(worldArea.Bottom));
                var gridTileRt = new MapIndices((int)Math.Floor(worldArea.Right), (int)Math.Floor(worldArea.Top));

                var tiles = new List<TileRef>();

                for (var x = gridTileLb.X; x <= gridTileRt.X; x++)
                {
                    for (var y = gridTileLb.Y; y <= gridTileRt.Y; y++)
                    {
                        var gridChunk = GridTileToGridChunk(new MapIndices(x, y));
                        if (Chunks.TryGetValue(gridChunk, out var chunk))
                        {
                            var chunkTile = chunk.GridTileToChunkTile(new MapIndices(x, y));
                            var tile = chunk.GetTile((ushort)chunkTile.X, (ushort)chunkTile.Y);

                            if (ignoreEmpty && tile.Tile.IsEmpty)
                                continue;

                            if (predicate == null || predicate(tile))
                            {
                                tiles.Add(tile);
                            }
                        }
                        else if (!ignoreEmpty)
                        {
                            var tile = new TileRef(MapId, Index, x, y, new Tile());

                            if (predicate == null || predicate(tile))
                            {
                                tiles.Add(tile);
                            }
                        }
                    }
                }
                return tiles;
            }

            #endregion TileAccess

            #region ChunkAccess

            /// <summary>
            /// The total number of allocated chunks in the grid.
            /// </summary>
            public int ChunkCount => Chunks.Count;

            /// <inheritdoc />
            public IMapChunk GetChunk(int xIndex, int yIndex)
            {
                return GetChunk(new MapIndices(xIndex, yIndex));
            }

            /// <inheritdoc />
            public IMapChunk GetChunk(MapIndices chunkIndices)
            {
                if (Chunks.TryGetValue(chunkIndices, out var output))
                    return output;

                return Chunks[chunkIndices] = new Chunk(_mapManager, this, chunkIndices.X, chunkIndices.Y, ChunkSize);
            }

            /// <inheritdoc />
            public IEnumerable<IMapChunk> GetMapChunks()
            {
                foreach (var kvChunk in Chunks)
                {
                    yield return kvChunk.Value;
                }
            }

            #endregion ChunkAccess

            #region SnapGridAccess

            /// <inheritdoc />
            public IEnumerable<SnapGridComponent> GetSnapGridCell(GridCoordinates worldPos, SnapGridOffset offset)
            {
                return GetSnapGridCell(SnapGridCellFor(worldPos, offset), offset);
            }

            /// <inheritdoc />
            public IEnumerable<SnapGridComponent> GetSnapGridCell(MapIndices pos, SnapGridOffset offset)
            {
                var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);
                return chunk.GetSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, offset);
            }

            /// <inheritdoc />
            public MapIndices SnapGridCellFor(GridCoordinates worldPos, SnapGridOffset offset)
            {
                var local = worldPos.ConvertToGrid(_mapManager, this);
                if (offset == SnapGridOffset.Edge)
                {
                    local = local.Offset(new Vector2(TileSize / 2f, TileSize / 2f));
                }
                var x = (int)Math.Floor(local.Position.X / TileSize);
                var y = (int)Math.Floor(local.Position.Y / TileSize);
                return new MapIndices(x, y);
            }

            /// <inheritdoc />
            public void AddToSnapGridCell(MapIndices pos, SnapGridOffset offset, SnapGridComponent snap)
            {
                var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);
                chunk.AddToSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, offset, snap);
            }

            /// <inheritdoc />
            public void AddToSnapGridCell(GridCoordinates worldPos, SnapGridOffset offset, SnapGridComponent snap)
            {
                AddToSnapGridCell(SnapGridCellFor(worldPos, offset), offset, snap);
            }

            /// <inheritdoc />
            public void RemoveFromSnapGridCell(MapIndices pos, SnapGridOffset offset, SnapGridComponent snap)
            {
                var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);
                chunk.RemoveFromSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, offset, snap);
            }

            /// <inheritdoc />
            public void RemoveFromSnapGridCell(GridCoordinates worldPos, SnapGridOffset offset, SnapGridComponent snap)
            {
                RemoveFromSnapGridCell(SnapGridCellFor(worldPos, offset), offset, snap);
            }
            
            private (IMapChunk, MapIndices) ChunkAndOffsetForTile(MapIndices pos)
            {
                var gridChunkIndices = GridTileToGridChunk(pos);
                var chunk = GetChunk(gridChunkIndices);
                var chunkTile = chunk.GridTileToChunkTile(pos);
                return (chunk, chunkTile);
            }

            #endregion

            #region Transforms

            /// <inheritdoc />
            public Vector2 WorldToLocal(Vector2 posWorld)
            {
                return posWorld - WorldPosition;
            }

            /// <inheritdoc />
            public GridCoordinates LocalToWorld(GridCoordinates posLocal)
            {
                return new GridCoordinates(posLocal.Position + WorldPosition, _mapManager.GetGrid(posLocal.GridID).ParentMap);
            }

            /// <inheritdoc />
            public Vector2 ConvertToWorld(Vector2 localPos)
            {
                return localPos + WorldPosition;
            }

            /// <summary>
            /// Transforms global world coordinates to tile indices relative to grid origin.
            /// </summary>
            /// <returns></returns>
            private MapIndices WorldToTile(GridCoordinates posWorld)
            {
                var local = posWorld.ConvertToGrid(_mapManager, this);
                var x = (int)Math.Floor(local.Position.X / TileSize);
                var y = (int)Math.Floor(local.Position.Y / TileSize);
                return new MapIndices(x, y);
            }

            /// <summary>
            /// Transforms global world coordinates to chunk indices relative to grid origin.
            /// </summary>
            /// <param name="posWorld">The position in the world.</param>
            /// <returns></returns>
            private MapIndices WorldToChunk(GridCoordinates posWorld)
            {
                var local = posWorld.ConvertToGrid(_mapManager, this);
                var x = (int)Math.Floor(local.Position.X / (TileSize * ChunkSize));
                var y = (int)Math.Floor(local.Position.Y / (TileSize * ChunkSize));
                return new MapIndices(x, y);
            }

            /// <inheritdoc />
            public MapIndices GridTileToGridChunk(MapIndices gridTile)
            {
                var x = (int)Math.Floor(gridTile.X / (float)ChunkSize);
                var y = (int)Math.Floor(gridTile.Y / (float)ChunkSize);

                return new MapIndices(x, y);
            }

            /// <inheritdoc />
            public GridCoordinates GridTileToLocal(MapIndices gridTile)
            {
                return new GridCoordinates(gridTile.X * TileSize + (TileSize / 2), gridTile.Y * TileSize + (TileSize / 2), this);
            }

            /// <inheritdoc />
            public bool IndicesToTile(MapIndices indices, out TileRef tile)
            {
                var chunkIndices = new MapIndices(indices.X / ChunkSize, indices.Y / ChunkSize);
                if (!Chunks.ContainsKey(chunkIndices))
                {
                    tile = new TileRef(); //Nothing should ever use or access this, bool check should occur first
                    return false;
                }
                Chunk chunk = Chunks[chunkIndices];
                tile = chunk.GetTile(new MapIndices(indices.X % ChunkSize, indices.Y % ChunkSize));
                return true;
            }

            #endregion Transforms
        }
    }
}
