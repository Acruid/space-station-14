using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;

namespace Robust.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// Server side Physics System that updates once a tick to simulate the physics worlds.
    /// </summary>
    [UsedImplicitly]
    public class PhysicsSystem : EntitySystem
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            _physicsManager.SimulateWorlds(TimeSpan.FromSeconds(frameTime), false);
        }
    }
}
