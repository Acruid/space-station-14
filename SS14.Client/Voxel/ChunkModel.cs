using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mike;
using OpenTK;
using OpenTK.Graphics;
using SS14.Shared.Interfaces.Map;

namespace SS14.Client.Voxel
{
    /// <summary>
    /// Contains everything needed to draw a chunk with Mike.
    /// </summary>
    class ChunkModel : Mike.Graphics.Model
    {
        private IMapChunk _chunk;

        public ChunkModel(Context context, IMapChunk chunk) : base(context)
        {
            _chunk = chunk;
        }

        public void BuildMesh()
        {
            var size = _chunk.ChunkSize;

            for (var x = 0; x < size; x++)
            {
                for (var y = 0; y < size; y++)
                {
                    
                }
            }
        }
    }
}
