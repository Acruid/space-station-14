using System;
using System.Collections.Generic;
using Mike;
using OpenTK;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Voxel
{
    class MapRender
    {
        private IMapManager _map;
        private Context _context;

        // chunks that are ready to be drawn
        Dictionary<MapGrid.Indices, ChunkModel> _drawableChunks = new Dictionary<MapGrid.Indices, ChunkModel>();

        // chunks that need to be rebuilt
        Queue<MapGrid.Indices> _dirtyChunks = new Queue<MapGrid.Indices>(30);

        public MapRender(Context context, int gridID)
        {
            _context = context;
            GridID = gridID;
            _map = IoCManager.Resolve<IMapManager>();
            _map.OnTileChanged += OnTileChanged;
        }

        public int GridID { get; }

        private void OnTileChanged(int gridId, TileRef tileRef, Tile oldTile)
        {
            if(gridId != GridID)
                return;

            /*
            var grid = _map.GetGrid(gridId);
            var pos = tileRef.LocalPos;
            var chunk = grid.GetChunk(grid.)
            */
        }

        private void BuildChunk(MapGrid.Indices indices)
        {
            throw new NotImplementedException();
        }

        // retrieves a chunk, queuing it up for rebuild if needed.
        private ChunkModel GetChunkModel(MapGrid.Indices indices)
        {
            // if the chunk can be drawn, return it
            if (_drawableChunks.TryGetValue(indices, out ChunkModel model))
                return model;

            // otherwise make the new chunk, and queue it up for rebuilding
            model = new ChunkModel(_context, _map.GetGrid(GridID).GetChunk(indices));
            _drawableChunks.Add(indices, model);
            _dirtyChunks.Enqueue(indices);
            
            return model;
        }

        // draws available grids to the scene
        public void DrawGrid(int gridID, Box2 worldViewBounds)
        {
            //TODO: cull drawable chunks to the view frustum
            
            if(!_map.TryGetGrid(gridID, out IMapGrid grid))
                return;

            var chunkDim = 1 / (grid.ChunkSize * (float)_map.TileSize);

            var chunkBounds = new Box2i(
                    (int)Math.Floor(worldViewBounds.Left * chunkDim),
                    (int)Math.Floor(worldViewBounds.Top * chunkDim),
                    (int)Math.Floor(worldViewBounds.Right * chunkDim),
                    (int)Math.Floor(worldViewBounds.Bottom * chunkDim)
            );

            for (var x = chunkBounds.Left; x <= chunkBounds.Right; x++)
            {
                for (var y = chunkBounds.Top; y <= chunkBounds.Bottom; y++)
                {
                    var model = GetChunkModel(new MapGrid.Indices(x, y));

                    model.Draw();

                }
            }
        }

        // updates tasks for the renderer
        public void Update()
        {
            // purge drawable cache of oldest chunks

            // pull from dirty chunks and build them
        }
    }
}
