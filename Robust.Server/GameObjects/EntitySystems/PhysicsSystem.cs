using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Robust.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class PhysicsSystem : SharedPhysicsSystem
    {
        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, false);
        }
    }
}
