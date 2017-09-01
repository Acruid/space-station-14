using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Mike
{
    public class Context
    {
        private GameWindow _wind;
        private Color4 _clearColor;

        internal Context(GameWindow window)
        {
            _wind = window;
            ClearColor = new Color4(51, 127, 178, 255);
        }

        public Color4 ClearColor
        {
            get => _clearColor;
            set
            {
                _clearColor = value;
                GL.ClearColor(_clearColor.R, _clearColor.G, _clearColor.B, _clearColor.A);
            }
        }
    }
}
