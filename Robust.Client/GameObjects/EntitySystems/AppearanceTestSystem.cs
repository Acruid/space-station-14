using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal class AppearanceTestSystem : EntitySystem
    {
        private readonly TypeEntityQuery<AppearanceTestComponent> _query
            = new TypeEntityQuery<AppearanceTestComponent>();

        public override void Update(float frameTime)
        {
            foreach (var animation in _query.EnumerateEntities(EntityManager))
            {
                animation.OnUpdate(frameTime);
            }
        }
    }
}
