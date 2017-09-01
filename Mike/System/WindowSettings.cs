namespace Mike.System
{
    /// <summary>
    /// Contains the configuration of a new window.
    /// </summary>
    public class WindowSettings
    {
        public ushort Width { get; set; } = 800;

        public ushort Height { get; set; } = 600;

        public string Title { get; set; } = "Mike OpenGL Window";
    }
}
