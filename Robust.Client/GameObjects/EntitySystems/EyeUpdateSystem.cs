using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.GameObjects.EntitySystems
{
    /// <summary>
    /// Updates the position of every Eye every frame, so that the camera follows the player around.
    /// </summary>
    [UsedImplicitly]
    internal class EyeUpdateSystem : EntitySystem
    {
        private readonly TypeEntityQuery<EyeComponent> _query
            = new TypeEntityQuery<EyeComponent>();

        /// <inheritdoc />
        public override void FrameUpdate(float frameTime)
        {
            foreach (var eye in _query.EnumerateEntities(EntityManager))
            {
                eye.UpdateEyePosition();
            }
        }
    }
}
