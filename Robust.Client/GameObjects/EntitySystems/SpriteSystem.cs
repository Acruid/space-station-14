﻿using JetBrains.Annotations;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SpriteSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IClyde _clyde;
        [Dependency] private readonly IEyeManager _eyeManager;
#pragma warning restore 649

        private readonly TypeEntityQuery<ISpriteComponent> _query
            = new TypeEntityQuery<ISpriteComponent>();

        public override void FrameUpdate(float frameTime)
        {
            var eye = _eyeManager.CurrentEye;

            // So we could calculate the correct size of the entities based on the contents of their sprite...
            // Or we can just assume that no entity is larger than 10x10 and get a stupid easy check.
            // TODO: Make this check more accurate.
            var worldBounds = Box2.CenteredAround(eye.Position.Position,
                _clyde.ScreenSize / EyeManager.PIXELSPERMETER * eye.Zoom).Enlarged(5);

            foreach (var sprite in _query.EnumerateEntities(EntityManager))
            {
                var transform = sprite.Owner.Transform;
                if (!worldBounds.Contains(transform.WorldPosition))
                {
                    continue;
                }

                // TODO: Don't call this on components without RSIs loaded.
                // Serious performance benefit here.
                sprite.FrameUpdate(frameTime);
            }
        }
    }
}
