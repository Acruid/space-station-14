using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mike.Graphics
{
    public class Model
    {
        // key: texture name, val: mesh to draw
        public Dictionary<string, Mike.Graphics.Mesh> _meshes;

        public List<string> _textures;
    }
}
