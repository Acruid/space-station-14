using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.Map;

namespace SS14.Client.Voxel
{
    class MapRender
    {
        // chunks that are ready to be drawn
        Dictionary<MapGrid.Indices, ChunkModel> _drawableChunks;

        // chunks that need to be rebuilt
        Dictionary<MapGrid.Indices, ChunkModel> _dirtyChunks;

        // chunks that need to be created
        Queue<MapGrid.Indices> _newChunks;


        private void BuildChunk(MapGrid.Indices indices)
        {
            throw new NotImplementedException();
        }

        // retrieves or queues up chunks
        private ChunkModel FetchChunkModel(MapGrid.Indices indices)
        {
            throw new NotImplementedException();

            // if the chunk is drawable
            // return it

            // if the chunk is dirty
            // dispatch rebuild it

            // if the chunk is new
            // dispatch build it

            // return null
        }

        public void DrawMap()
        {
            throw new NotImplementedException();

            // cull drawable chunks to the view frustum

            // for each chunk in the view

            // fetch the model

            // draw it
        }
    }
}
