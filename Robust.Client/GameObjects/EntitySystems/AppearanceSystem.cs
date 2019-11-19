using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class AppearanceSystem : EntitySystem
    {
        private readonly TypeEntityQuery<AppearanceComponent> _query
            = new TypeEntityQuery<AppearanceComponent>();

        public override void FrameUpdate(float frameTime)
        {
            foreach (var appearance in _query.EnumerateEntities(EntityManager))
            {
                if (!appearance.AppearanceDirty)
                    continue;

                UpdateComponent(appearance);
                appearance.AppearanceDirty = false;
            }
        }

        private static void UpdateComponent(AppearanceComponent component)
        {
            foreach (var visualizer in component.Visualizers)
            {
                switch (visualizer)
                {
                    case AppearanceComponent.SpriteLayerToggle spriteLayerToggle:
                        UpdateSpriteLayerToggle(component, spriteLayerToggle);
                        break;

                    default:
                        visualizer.OnChangeData(component);
                        break;
                }
            }
        }

        private static void UpdateSpriteLayerToggle(AppearanceComponent component, AppearanceComponent.SpriteLayerToggle toggle)
        {
            component.TryGetData(toggle.Key, out bool visible);
            var sprite = component.Owner.GetComponent<SpriteComponent>();
            sprite.LayerSetVisible(toggle.SpriteLayer, visible);
        }
    }
}
