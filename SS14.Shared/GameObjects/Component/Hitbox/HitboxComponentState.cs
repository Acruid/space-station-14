using SS14.Shared.Maths;
using System;

namespace SS14.Shared.GameObjects.Components.Hitbox
{
    /// <summary>
    /// Hitbox is defined as the hight/width Size and pixel offset.
    /// The center of the hitbox is on the center of the entity it is attached to.
    /// The offset adjusts the position of the center of the hitbox.
    /// </summary>
    [Serializable]
    public class HitboxComponentState : ComponentState
    {
        public RectF AABB;

        public HitboxComponentState(RectF aabb)
            :base(ComponentFamily.Hitbox)
        {
            AABB = aabb;
        }
    }
}
