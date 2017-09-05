using System.Collections.Generic;
using OpenTK;

namespace Mike.Graphics
{
    public class Model
    {
        private Context _context;

        private Matrix4 modelMatrix = Matrix4.Identity;

        private Vector3 _translation = Vector3.Zero;
        private Quaternion _rotation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;

        public Vector3 Translation { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public Matrix4 ModelMatrix { get; set; }

        public List<Mesh> Meshes { get; set; } = new List<Mesh>();

        public Model(Context context)
        {
            _context = context;
        }
        
        public virtual void Draw()
        {
            if(Meshes == null)
                return;
        
            var mvpMatrix = ModelMatrix * _context.VPMatrix;
            _context.CurrentShader.SetUniformMatrix4("transform", false, ref mvpMatrix);

            foreach (var mesh in Meshes)
            {
                mesh.Vao.Render();
            }
        }
    }
}
