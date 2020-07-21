using System;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;

namespace Robust.Shared.GameObjects.Systems
{
    public abstract class SharedPhysicsSystem : EntitySystem
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        protected void SimulateWorld(float frameTime, bool prediction)
        {
            _physicsManager.SimulateWorld(TimeSpan.FromSeconds(frameTime), prediction);
        }
    }
}
