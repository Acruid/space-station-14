using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class AnimationPlayerSystem : EntitySystem
    {
        private readonly TypeEntityQuery<AnimationPlayerComponent> _query
            = new TypeEntityQuery<AnimationPlayerComponent>();

        public override void FrameUpdate(float frameTime)
        {
            foreach (var player in _query.EnumerateEntities(EntityManager))
            {
                player.Update(frameTime);
            }
        }
    }
}
