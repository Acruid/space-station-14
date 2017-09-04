using OpenTK;
using OpenTK.Graphics;
using SFML.Window;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Interfaces;
using SS14.Client.State.States;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Mike.Graphics;
using OpenTK.Graphics.OpenGL;
using Render.Loader;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Timing;
using KeyArgs = SFML.Window.KeyEventArgs;
using SS14.Shared.Network.Messages;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameStates;
using Color = SFML.Graphics.Color;
using PrimitiveType = OpenTK.Graphics.OpenGL.PrimitiveType;
using Texture = Mike.Graphics.Texture;

namespace SS14.Client
{
    public class GameController : IGameController
    {
        #region Fields

        [Dependency]
        readonly private IConfigurationManager _configurationManager;
        [Dependency]
        readonly private INetworkGrapher _netGrapher;
        [Dependency]
        readonly private IClientNetManager _networkManager;
        [Dependency]
        readonly private IStateManager _stateManager;
        [Dependency]
        readonly private IUserInterfaceManager _userInterfaceManager;
        [Dependency]
        readonly private IResourceCache _resourceCache;
        [Dependency]
        readonly private ITileDefinitionManager _tileDefinitionManager;
        [Dependency]
        readonly private ISS14Serializer _serializer;
        [Dependency]
        private readonly IGameTiming _time;
        [Dependency]
        private readonly IResourceManager _resourceManager;
        [Dependency]
        private readonly IMapManager _mapManager;

        #endregion Fields

        #region Methods

        #region Constructors

        private TimeSpan _lastTick;
        private TimeSpan _lastKeepUpAnnounce;

#if !CL
        public static Mike.System.Window Wind { get; private set; }

        private ShaderProgram _shader;
        private readonly Dictionary<string, Mike.Graphics.Texture> _textures = new Dictionary<string, Texture>();

        private Mike.Graphics.Mesh _model;

        private Camera _cam;

        /*
        float[] vertices = {
            // positions          // colors           // texture coords
            0.5f,  0.5f, 0.0f,   1.0f, 0.0f, 0.0f,   1.0f, 1.0f,   // top right
            0.5f, -0.5f, 0.0f,   0.0f, 1.0f, 0.0f,   1.0f, 0.0f,   // bottom right
            -0.5f, -0.5f, 0.0f,   0.0f, 0.0f, 1.0f,   0.0f, 0.0f,   // bottom left
            -0.5f,  0.5f, 0.0f,   1.0f, 1.0f, 0.0f,   0.0f, 1.0f    // top left 
        };
        */

        // opengl winds triangles counter clockwise by default
        private readonly Vector3[] verts =
        {
            new Vector3( 0.5f,  0.5f, 0.0f), // top right
            new Vector3( 0.5f, -0.5f, 0.0f), // bottom right
            new Vector3(-0.5f, -0.5f, 0.0f), // bottom left
            new Vector3(-0.5f,  0.5f, 0.0f), // top left 
        };

        private readonly Vector3[] colors =
        {
            new Vector3(1.0f, 0.0f, 0.0f), // top right
            new Vector3(0.0f, 1.0f, 0.0f), // bottom right
            new Vector3(0.0f, 0.0f, 1.0f), // bottom left
            new Vector3(1.0f, 1.0f, 0.0f), // top left 
        };

        private readonly Vector2[] tex =
        {
            new Vector2(1.0f, 0.0f), // top right
            new Vector2(1.0f, 1.0f), // bottom right
            new Vector2(0.0f, 1.0f), // bottom left
            new Vector2(0.0f, 0.0f), // top left 
        };

        Vector3[] cubePositions = {
            new Vector3( 0.0f,  0.0f,  0.0f),
            new Vector3( 2.0f,  5.0f, -15.0f),
            new Vector3(-1.5f, -2.2f, -2.5f),
            new Vector3(-3.8f, -2.0f, -12.3f),
            new Vector3( 2.4f, -0.4f, -3.5f),
            new Vector3(-1.7f,  3.0f, -7.5f),
            new Vector3( 1.3f, -2.0f, -2.5f),
            new Vector3( 1.5f,  2.0f, -2.5f),
            new Vector3( 1.5f,  0.2f, -1.5f),
            new Vector3(-1.3f,  1.0f, -1.5f)
        };

        public static Vector3 Up = new Vector3(0, 1, 0);
        public static Vector3 Center = new Vector3(0, 0, 0);
        public static Vector3 Spawn = new Vector3(0, 0, 3);

        private Matrix4 _vpMatrix;

#endif
        public void Run()
        {
            Logger.Debug("Initializing GameController.");
#if CL
            _configurationManager.LoadFromFile(PathHelpers.ExecutableRelativeFile("client_config.toml"));

            _resourceCache.LoadBaseResources();
            // Load resources used by splash screen and main menu.
            LoadSplashResources();
            ShowSplashScreen();

            _resourceCache.LoadLocalResources();

            LoadContentAssembly<GameShared>("Shared");
            LoadContentAssembly<GameClient>("Client");

            // Call Init in game assemblies.
            AssemblyLoader.BroadcastRunLevel(AssemblyLoader.RunLevel.Init);

            //Setup Cluwne first, as the rest depends on it.
            SetupCluwne();
            CleanupSplashScreen();

            //Initialization of private members
            _tileDefinitionManager.InitializeResources();

            _serializer.Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadDirectory(@"Prototypes");
            prototypeManager.Resync();
            _networkManager.Initialize(false);
            _netGrapher.Initialize();
            _userInterfaceManager.Initialize();
            _mapManager.Initialize();
            
            _networkManager.RegisterNetMessage<MsgFullState>(MsgFullState.NAME, (int)MsgFullState.ID, message => IoCManager.Resolve<IGameStateManager>().HandleFullStateMessage((MsgFullState)message));
            _networkManager.RegisterNetMessage<MsgStateUpdate>(MsgStateUpdate.NAME, (int)MsgStateUpdate.ID, message => IoCManager.Resolve<IGameStateManager>().HandleStateUpdateMessage((MsgStateUpdate)message));
            _networkManager.RegisterNetMessage<MsgEntity>(MsgEntity.NAME, (int)MsgEntity.ID, message => IoCManager.Resolve<IClientEntityManager>().HandleEntityNetworkMessage((MsgEntity)message));

            _stateManager.RequestStateChange<MainScreen>();

            #region GameLoop

            // maximum number of ticks to queue before the loop slows down.
            const int maxTicks = 5;

            _time.ResetRealTime();
            var maxTime = TimeSpan.FromTicks(_time.TickPeriod.Ticks * maxTicks);

            while (CluwneLib.IsRunning)
            {
                var accumulator = _time.RealTime - _lastTick;

                // If the game can't keep up, limit time.
                if (accumulator > maxTime)
                {
                    // limit accumulator to max time.
                    accumulator = maxTime;

                    // pull lastTick up to the current realTime
                    // This will slow down the simulation, but if we are behind from a
                    // lag spike hopefully it will be able to catch up.
                    _lastTick = _time.RealTime - maxTime;

                    // announce we are falling behind
                    if ((_time.RealTime - _lastKeepUpAnnounce).TotalSeconds >= 15.0)
                    {
                        Logger.Warning("[SRV] MainLoop: Cannot keep up!");
                        _lastKeepUpAnnounce = _time.RealTime;
                    }
                }

                _time.StartFrame();

                var realFrameEvent = new FrameEventArgs((float)_time.RealFrameTime.TotalSeconds);

                // process Net/KB/Mouse input
                Process(realFrameEvent);

                _time.InSimulation = true;
                // run the simulation for every accumulated tick
                while (accumulator >= _time.TickPeriod)
                {
                    accumulator -= _time.TickPeriod;
                    _lastTick += _time.TickPeriod;

                    // only run the sim if unpaused, but still use up the accumulated time
                    if (!_time.Paused)
                    {
                        // update the simulation
                        var simFrameEvent = new FrameEventArgs((float) _time.FrameTime.TotalSeconds);
                        Update(simFrameEvent);
                        _time.CurTick++;
                    }
                }

                // if not paused, save how close to the next tick we are so interpolation works
                if (!_time.Paused)
                    _time.TickRemainder = accumulator;

                _time.InSimulation = false;

                // render the simulation
                Render(realFrameEvent);
            }

            #endregion

            _networkManager.ClientDisconnect("Client disconnected from game.");
            CluwneLib.Terminate();
            Logger.Info("GameController terminated.");

            IoCManager.Resolve<IConfigurationManager>().SaveToFile();
#else
            // make a new setting object to hold window settings.
            var settings = new Mike.System.WindowSettings();
            settings.Width = 600;
            settings.Height = 600;
            settings.Title = "Space Station 14";

            // create the new window
            Wind = new Mike.System.Window(settings);

            // called when the window is loaded, but before it is actually shown
            // the GraphicsContext is created at this point, so set it up how you want it
            Wind.Load += (sender, args) =>
            {
                // make a new shader program
                var shader = new ShaderProgram();

                // add the Vertex and Fragment shaders to our program
                shader.Add(new Mike.Graphics.Shader(ShaderType.VertexShader, new FileInfo(@"Graphics/Shaders/vert_textured.gls")));
                shader.Add(new Mike.Graphics.Shader(ShaderType.FragmentShader, new FileInfo(@"Graphics/Shaders/frag_textured.gls")));

                // compile the program
                shader.Compile();
                _shader = shader;

                // use this shader program for drawing VBO's from now on
                // you can call this in Render event to swap between multiple shader programs,
                // but for now we just have one, so no point re-binding it every frame.
                _shader.Use();

                _cam = new Mike.Graphics.Camera(new Size(800, 800),  Spawn, Center, Up);

                // parse in an OBJ model
                var model = LoaderOBJ.Create(new FileInfo(@"Loader/Examples/cube.obj"));

                _model = new Mesh();

                //TODO: The LoaderObj class needs to do this.
                // load the texture of the cube
                {
                    foreach (var kvMaterial in model.Materials)
                    {
                        var diffuseFile = new FileInfo(kvMaterial.Value.diffuseMap);

                        if(!diffuseFile.Exists)
                            throw new FileNotFoundException("File does not exist: " + diffuseFile.FullName);

                        var tex = Texture.Create(diffuseFile);

                        _textures.Add(kvMaterial.Key, tex);
                    }
                }

                // we are only using texture 0 for now
                _model.Texture = _textures[model.Meshes[0].Material];

                //TODO: The LoaderOBJ class needs a function to do this, people should not have to
                // build the VAO for the model
                {
                    var mesh = model.Meshes[0]; // hard code mesh 0 for now
                    var numVerts = mesh.Faces.Count * 3;

                    // lookup the indexed verts and build the array of VBO verts
                    // this is an OBJ format specific thing
                    var count = 0;
                    var objVerts = mesh.CoordVerts;
                    var vboVerts = new Vector3[numVerts];
                    foreach (var face in mesh.Faces)
                    {
                        foreach (var pos in face.Pos)
                        {
                            vboVerts[count] = objVerts[(int) pos - 1]; // OBJ index starts at 1
                            count++;
                        }
                    }

                    // drawing all 3 points, and we have 3 verts / face
                    _model.Vao = new VAO(PrimitiveType.Triangles, 36);
                    _model.Vao.Use();

                    // make a VBO array to hold the actual triangle verts
                    var vertexVbo = new VBO();
                    vertexVbo.Buffer(BufferTarget.ArrayBuffer, mesh.CoordVerts.ToArray(), 3);
                    // add the VBO to the VAO at attribute location 0 in shader
                    _model.Vao.AddVBO(0, vertexVbo);

                    var colVbo = new VBO();
                    colVbo.Buffer(BufferTarget.ArrayBuffer, mesh.CoordNorm.ToArray(), 3);
                    _model.Vao.AddVBO(1, colVbo);

                    var uvVbo = new VBO();
                    uvVbo.Buffer(BufferTarget.ArrayBuffer, mesh.CoordTex.ToArray(), 2);
                    _model.Vao.AddVBO(2, uvVbo);

                    // Other state
                    GL.Enable(EnableCap.DepthTest);
                }
            };

            // called from the GameWindow main loop, this is for updating logic.
            Wind.Update += (sender, args) =>
            {
                _cam.Think(args.Time);
                
                var viewMatrix = _cam.ViewMatrix;
                var projMatrix = _cam.ProjectionMatrix;
                
                // apply matrix to the shader
                // OPENTK MATRICES ARE ROW MAJOR, NOT COLUMN MAJOR, MULTIPLY THEM PROPERLY
                //var MvpMatrix = projMatrix * viewMatrix * modelMatrix;
               _vpMatrix = viewMatrix * projMatrix;

                //_shader.SetUniformMatrix4("transform", false, ref MvpMatrix);
            };

            // called from the GameWindow main loop, this is for drawing the frame to the screen.
            Wind.Draw += (sender, args) =>
            {
                var texID = _model.Texture.ID;
                _model.Texture.BindTexture2d(ref texID, Wind.Context.GetTexUnit(0));
                _shader.SetUniformTexture("ourTexture", Wind.Context.GetTexUnit(0));
                _model.Vao.Use();

                GL.FrontFace(FrontFaceDirection.Cw);
                //GL.Disable(EnableCap.CullFace);

                for(var i = 0; i < cubePositions.Length;i++)
                {
                    var modelMatrix = Matrix4.Identity;

                    modelMatrix = Matrix4.CreateTranslation(cubePositions[i]) * modelMatrix;

                    float angle = 20.0f * i;
                    modelMatrix = Matrix4.CreateFromAxisAngle(new Vector3(1.0f, 0.3f, 0.5f), MathHelper.DegreesToRadians(angle)) * modelMatrix;

                    var mvpMatrix = modelMatrix * _vpMatrix;
                    _shader.SetUniformMatrix4("transform", false, ref mvpMatrix);
                    _model.Vao.Render();
                }
                
                //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                _model.Vao.Render();
                
                //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
                //GL.PointSize(4);
                //_model.Vao.Render();

            };

            // called from GameWindow when it is about to be destroyed, clean up stuff here.
            Wind.Unload += (sender, args) => { };

            // actually show the window after everything is set up.
            Wind.Show();
#endif
        }

        private void LoadContentAssembly<T>(string name) where T: GameShared
        {
            // get the assembly from the file system
            if (_resourceManager.TryContentFileRead($@"Assemblies/Content.{name}.dll", out MemoryStream gameDll))
            {
                Logger.Debug($"[SRV] Loading {name} Content DLL");

                // see if debug info is present
                if (_resourceManager.TryContentFileRead($@"Assemblies/Content.{name}.pdb", out MemoryStream gamePdb))
                {
                    try
                    {
                        // load the assembly into the process, and bootstrap the GameServer entry point.
                        AssemblyLoader.LoadGameAssembly<T>(gameDll.ToArray(), gamePdb.ToArray());
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"[SRV] Exception loading DLL Content.{name}.dll: {e}");
                    }
                }
                else
                {
                    try
                    {
                        // load the assembly into the process, and bootstrap the GameServer entry point.
                        AssemblyLoader.LoadGameAssembly<T>(gameDll.ToArray());
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"[SRV] Exception loading DLL Content.{name}.dll: {e}");
                    }
                }
            }
            else
            {
                Logger.Warning($"[ENG] Could not find {name} Content DLL");
            }
        }

        /// <summary>
        /// Processes all simulation I/O. Keyboard/Mouse/Network code gets called here.
        /// </summary>
        private void Process(FrameEventArgs e)
        {
            //TODO: Keyboard/Mouse input needs to be processed here.

        }

        /// <summary>
        /// Runs a tick of the simulation.
        /// </summary>
        /// <param name="e">Current GameTiming.FrameTime</param>
        private void Update(FrameEventArgs e)
        {
            _networkManager.ProcessPackets();
            CluwneLib.RunIdle(this, e);
            _stateManager.Update(e);
        }

        /// <summary>
        /// Renders the view of the simulation.
        /// </summary>
        /// <param name="e">Current GameTiming.RealFrameTime</param>
        private void Render(FrameEventArgs e)
        {
            CluwneLib.ClearCurrentRendertarget(Color4.Black);
            CluwneLib.Window.DispatchEvents();

            // draw everything
            _stateManager.Render(e);

            // interface runs in realtime, so it is updated here
            _userInterfaceManager.Update(e);

            _userInterfaceManager.Render(e);

            _netGrapher.Update();

            // swap buffers to show the screen
            CluwneLib.Window.Graphics.Display();
        }

        private void LoadSplashResources()
        {
            var logoTexture = _resourceCache.LoadTextureFrom("ss14_logo", _resourceManager.ContentFileRead(@"Textures/Logo/logo.png"));
            _resourceCache.LoadSpriteFromTexture("ss14_logo", logoTexture);

            var backgroundTexture = _resourceCache.LoadTextureFrom("ss14_logo_background", _resourceManager.ContentFileRead(@"Textures/Logo/background.png"));
            _resourceCache.LoadSpriteFromTexture("ss14_logo_background", backgroundTexture);

            var nanotrasenTexture = _resourceCache.LoadTextureFrom("ss14_logo_nt", _resourceManager.ContentFileRead(@"Textures/Logo/nanotrasen.png"));
            _resourceCache.LoadSpriteFromTexture("ss14_logo_nt", nanotrasenTexture);
        }

        [Conditional("RELEASE")]
        private void ShowSplashScreen()
        {
            // Do nothing when we're on DEBUG builds.
            // The splash is just annoying.
            const uint SIZE_X = 600;
            const uint SIZE_Y = 300;
            // Size of the NT logo in the bottom right.
            const float NT_SIZE_X = SIZE_X / 10f;
            const float NT_SIZE_Y = SIZE_Y / 10f;
            var window = CluwneLib.ShowSplashScreen(new VideoMode(SIZE_X, SIZE_Y)).Graphics;

            var logo = _resourceCache.GetSprite("ss14_logo");
            logo.Position = new Vector2f(SIZE_X / 2 - logo.TextureRect.Width / 2, SIZE_Y / 2 - logo.TextureRect.Height / 2);

            var background = _resourceCache.GetSprite("ss14_logo_background");
            background.Scale = new Vector2f((float)SIZE_X / background.TextureRect.Width, (float)SIZE_Y / background.TextureRect.Height);

            var nanotrasen = _resourceCache.GetSprite("ss14_logo_nt");
            nanotrasen.Scale = new Vector2f(NT_SIZE_X / nanotrasen.TextureRect.Width, NT_SIZE_Y / nanotrasen.TextureRect.Height);
            nanotrasen.Position = new Vector2f(SIZE_X - NT_SIZE_X - 5, SIZE_Y - NT_SIZE_Y - 5);
            nanotrasen.Color = new Color(255, 255, 255, 64);

            window.Draw(background);
            window.Draw(logo);
            window.Draw(nanotrasen);
            window.Display();
        }

        [Conditional("RELEASE")]
        private void CleanupSplashScreen()
        {
            CluwneLib.CleanupSplashScreen();
        }

#endregion Constructors

#region EventHandlers

        private void MainWindowLoad(object sender, EventArgs e)
        {
            _stateManager.RequestStateChange<MainScreen>();
        }

        private void MainWindowResizeEnd(object sender, SizeEventArgs e)
        {
            var view = new SFML.Graphics.View(
                new SFML.System.Vector2f(e.Width / 2, e.Height / 2),
                new SFML.System.Vector2f(e.Width, e.Height)
                );
            CluwneLib.Window.Camera.SetView(view);
            _stateManager.FormResize();
        }
        private void MainWindowRequestClose(object sender, EventArgs e)
        {
            CluwneLib.Stop();
        }

#region Input Handling

        /// <summary>
        /// Handles any keydown events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The KeyArgsinstance containing the event data.</param>
        private void KeyDownEvent(object sender, KeyArgs e)
        {
            if (_stateManager != null)
                _stateManager.KeyDown(e);

            switch (e.Code)
            {
                case Keyboard.Key.F3:
                    IoCManager.Resolve<INetworkGrapher>().Toggle();
                    break;
            }
        }

        /// <summary>
        /// Handles any keyup events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The KeyArgs instance containing the event data.</param>
        private void KeyUpEvent(object sender, KeyArgs e)
        {
            if (_stateManager != null)
                _stateManager.KeyUp(e);
        }

        /// <summary>
        /// Handles mouse wheel input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The MouseWheelEventArgs instance containing the event data.</param>
        private void MouseWheelMoveEvent(object sender, MouseWheelEventArgs e)
        {
            if (_stateManager != null)
                _stateManager.MouseWheelMove(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The MouseMoveEventArgs instance containing the event data.</param>
        private void MouseMoveEvent(object sender, MouseMoveEventArgs e)
        {
            if (_stateManager != null)
                _stateManager.MouseMove(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The MouseButtonEventArgs instance containing the event data.</param>
        private void MouseDownEvent(object sender, MouseButtonEventArgs e)
        {
            if (_stateManager != null)
                _stateManager.MouseDown(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The MouseButtonEventArgs instance containing the event data.</param>
        private void MouseUpEvent(object sender, MouseButtonEventArgs e)
        {
            if (_stateManager != null)
                _stateManager.MouseUp(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The EventArgs instance containing the event data.</param>
        private void MouseEntered(object sender, EventArgs e)
        {
            Cursor.Hide();
            if (_stateManager != null)
                _stateManager.MouseEntered(e);
        }

        /// <summary>
        /// Handles any mouse input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The EventArgs instance containing the event data.</param>
        private void MouseLeft(object sender, EventArgs e)
        {
            Cursor.Show();
            if (_stateManager != null)
                _stateManager.MouseLeft(e);
        }

        private void TextEntered(object sender, TextEventArgs e)
        {
            if (_stateManager != null)
                _stateManager.TextEntered(e);
        }

#endregion Input Handling

#endregion EventHandlers

#region Privates

        bool onetime = true;

        private void SetupCluwne()
        {
            _configurationManager.RegisterCVar("display.width", 1280, CVarFlags.ARCHIVE);
            _configurationManager.RegisterCVar("display.height", 720, CVarFlags.ARCHIVE);
            _configurationManager.RegisterCVar("display.fullscreen", false, CVarFlags.ARCHIVE);
            _configurationManager.RegisterCVar("display.refresh", 60, CVarFlags.ARCHIVE);
            _configurationManager.RegisterCVar("display.vsync", false, CVarFlags.ARCHIVE);

            uint displayWidth = (uint) _configurationManager.GetCVar<int>("display.width");
            uint displayHeight = (uint) _configurationManager.GetCVar<int>("display.height");
            bool isFullscreen = _configurationManager.GetCVar<bool>("display.fullscreen");
            uint refresh = (uint) _configurationManager.GetCVar<int>("display.refresh");

            CluwneLib.Video.SetFullScreen(isFullscreen);
            CluwneLib.Video.SetRefreshRate(refresh);
            CluwneLib.Video.SetWindowSize(displayWidth, displayHeight);
            CluwneLib.Initialize();
            if (onetime)
            {
                //every time the video settings change we close the old screen and create a new one
                //SetupCluwne Gets called to reset the event handlers to the new screen
                CluwneLib.RefreshVideoSettings += SetupCluwne;
                onetime = false;
            }
            CluwneLib.Window.SetMouseCursorVisible(false);
            CluwneLib.Window.Graphics.BackgroundColor = Color.Black;
            CluwneLib.Window.Resized += MainWindowResizeEnd;
            CluwneLib.Window.Closed += MainWindowRequestClose;
            CluwneLib.Input.KeyPressed += KeyDownEvent;
            CluwneLib.Input.KeyReleased += KeyUpEvent;
            CluwneLib.Input.MouseButtonPressed += MouseDownEvent;
            CluwneLib.Input.MouseButtonReleased += MouseUpEvent;
            CluwneLib.Input.MouseMoved += MouseMoveEvent;
            CluwneLib.Input.MouseWheelMoved += MouseWheelMoveEvent;
            CluwneLib.Input.MouseEntered += MouseEntered;
            CluwneLib.Input.MouseLeft += MouseLeft;
            CluwneLib.Input.TextEntered += TextEntered;

            CluwneLib.Go();
            IoCManager.Resolve<IKeyBindingManager>().Initialize();
        }

#endregion Privates

#endregion Methods
    }
}
