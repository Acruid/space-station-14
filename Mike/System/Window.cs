using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Mike.System
{
    /// <summary>
    ///     Manages the OS window that contains the graphics context.
    /// </summary>
    public class Window
    {
        public event EventHandler<EventArgs> Load; 
        public event EventHandler<FrameEventArgs> UpdateFrame;
        public event EventHandler<FrameEventArgs> RenderFrame;
        public event EventHandler<EventArgs> Unload; 

        private readonly GameWindow _window;

        public Window(WindowSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _window = new GameWindow(settings.Width, settings.Height, GraphicsMode.Default, settings.Title);

            Console.WriteLine("gl version: " + GL.GetString(StringName.Version));

            _window.Resize += OnResize;

            _window.Load += OnLoad;
            _window.UpdateFrame += OnUpdateFrame;
            _window.RenderFrame += OnRenderFrame;
            _window.Unload += OnUnload;

            _window.VSync = VSyncMode.Adaptive;
        }

        /// <summary>
        ///     OpenGL graphical context.
        /// </summary>
        public Context Context { get; private set; }

        /// <summary>
        ///     OpenGL viewport displayed in the window.
        /// </summary>
        public Viewport View { get; private set; }

        public void Initialize()
        {
            // actually starts the loop and displays the window
            _window.Run();
        }

        // this is called when the window starts running
        private void OnLoad(object sender, EventArgs eventArgs)
        {
            Context = new Context(_window);
            View = new Viewport(_window);

            Load?.Invoke(this, eventArgs);
        }

        // this is called every frame, put game logic here
        private void OnUpdateFrame(object sender, FrameEventArgs frameEventArgs)
        {
            UpdateFrame?.Invoke(this, frameEventArgs);
        }

        // this is called every frame, put game logic here
        private void OnRenderFrame(object sender, FrameEventArgs frameEventArgs)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            RenderFrame?.Invoke(this, frameEventArgs);

            _window.SwapBuffers();
        }

        // this is called when the window is resized
        private void OnResize(object sender, EventArgs eventArgs)
        {
            GL.Viewport(0, 0, _window.Width, _window.Height);
        }

        // this is called when the window is about to be destroyed
        private void OnUnload(object sender, EventArgs eventArgs)
        {
            Unload?.Invoke(this, eventArgs);
        }
    }
}
