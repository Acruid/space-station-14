using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///
    /// </summary>
    public interface IPhysBody
    {
        /// <summary>
        ///     Entity that this physBody represents.
        /// </summary>
        IEntity Owner { get; }

        /// <summary>
        ///     AABB of this entity in world space.
        /// </summary>
        Box2 WorldAABB { get; }

        /// <summary>
        ///     AABB of this entity in local space.
        /// </summary>
        Box2 AABB { get; }

        IList<IPhysShape> PhysicsShapes { get; }

        /// <summary>
        /// Whether or not this body can collide.
        /// </summary>
        bool CanCollide { get; set; }

        /// <summary>
        /// Bitmask of the collision layers this body is a part of. The layers are calculated from
        /// all of the shapes of this body.
        /// </summary>
        int CollisionLayer { get; }

        /// <summary>
        /// Bitmask of the layers this body collides with. The mask is calculated from
        /// all of the shapes of this body.
        /// </summary>
        int CollisionMask { get; }

        /// <summary>
        ///     The map index this physBody is located upon
        /// </summary>
        MapId MapID { get; }

        /// <summary>
        /// Broad Phase proxy ID.
        /// </summary>
        int ProxyId { get; set; }

        /// <summary>
        /// The type of the body, which determines how collisions effect this object.
        /// </summary>
        BodyType BodyType { get; set; }

        int SleepAccumulator { get; set; }

        int SleepThreshold { get; set; }

        bool Awake { get; }

        void WakeBody();

        #region Legacy

        [Obsolete("Temporary Compatibility with legacy code.")]
        bool IsDynamic(out IPhysicsComponent? physicsComp);

        [Obsolete("Temporary Compatibility with legacy code.")]
        bool IsDynamic();

        [Obsolete("Temporary Compatibility with legacy code.")]
        IPhysicsComponent PhysicsComponent { get; }

        [Obsolete("Temporary Compatibility with legacy code.")]
        void SetupPhysicsProxy();

        #endregion

        Vector2 Position { get; set; }
        float Rotation { get; set; }
        Vector2 LinearVelocity { get; set; }
        float AngularVelocity { get; set; }
        Vector2 Force { get; set; }
        float Torque { get; set; }
        float Mass { get; set; }
        float I { get; set; }
        float InvMass { get; set; }
        float InvI { get; set; }
    }
}
