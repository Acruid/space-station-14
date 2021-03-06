using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Client.Interfaces.GameObjects
{
    /// <summary>
    /// A component that can be clicked on. Handles whether a coordinate is a valid place to click us on.
    /// </summary>
    public interface IClickTargetComponent : IComponent
    {
        bool WasClicked(Vector2f worldPos);
        DrawDepth DrawDepth { get; }
    }
}
