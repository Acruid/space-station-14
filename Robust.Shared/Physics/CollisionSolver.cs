using System;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    internal static class CollisionSolver
    {
        public static void CalculateFeatures(Manifold manifold, IPhysShape a, IPhysShape b, out CollisionFeatures features)
        {
            // 2D table of all possible PhysShape combinations
            switch (a)
            {
                case PhysShapeCircle aCircle:
                    switch (b)
                    {
                        case PhysShapeCircle bCircle:
                            CircleCircle(manifold, aCircle, bCircle, out features);
                            return;
                    }
                    break;
            }

            features = default;
        }

        private static void CircleCircle(Manifold manifold, PhysShapeCircle a, PhysShapeCircle b, out CollisionFeatures features)
        {
            var aRad = a.Radius;
            var bRad = b.Radius;

            var aPos = manifold.A.Entity.Transform.WorldPosition;
            var bPos = manifold.B.Entity.Transform.WorldPosition;

            CalculateCollisionFeatures(new Circle(aPos, aRad), new Circle(bPos, bRad), out features);
        }

        /// <summary>
        /// Calculates the collision features between two circles.
        /// </summary>
        /// <param name="a">The first circle to consider.</param>
        /// <param name="b">The second circle to consider.</param>
        /// <param name="features">The calculated features of this collision.</param>
        public static void CalculateCollisionFeatures(in Circle a, in Circle b, out CollisionFeatures features)
        {
            var aRad = a.Radius;
            var bRad = b.Radius;

            var aPos = a.Position;
            var bPos = b.Position;

            // combined radius
            var radiiSum = aRad + bRad;

            // distance between circles
            var dist = bPos - aPos;

            // if the distance between two circles is larger than their combined radii,
            // they are not colliding, otherwise they are
            if (dist.LengthSquared > radiiSum * radiiSum)
            {
                features = default;
                return;
            }

            // if dist between circles is zero, this collision cannot be resolved
            if (dist.LengthSquared.Equals(0f))
            {
                features = default;
                return;
            }

            // generate collision normal
            var normal = dist.Normalized;

            // half of the total 
            var penetraction = (radiiSum - dist.Length) * 0.5f;

            var contacts = new Vector2[1];

            // dtp - Distance to intersection point
            var dtp = aRad - penetraction;
            var contact = aPos + dist * dtp;
            contacts[0] = contact;

            features = new CollisionFeatures(true, normal, penetraction, contacts);
        }
    }

    /// <summary>
    /// Features of the collision.
    /// </summary>
    internal readonly struct CollisionFeatures
    {
        /// <summary>
        /// Are the two shapes *actually* colliding? If this is false, the rest of the
        /// values in this struct are default.
        /// </summary>
        public readonly bool Collided;

        /// <summary>
        /// Collision normal. If A moves in the negative direction of the normal and
        /// B moves in the positive direction, the objects will no longer intersect.
        /// </summary>
        public readonly Vector2 Normal;

        /// <summary>
        /// Half of the total length of penetration. Each object needs to move
        /// by the penetration distance along the normal to resolve the collision.
        /// </summary>
        public readonly float Penetration;

        /// <summary>
        /// all the points at which the two objects collide, projected onto a plane.The plane
        /// these points are projected onto has the normal of the collision normal and is
        /// located halfway between the colliding objects.
        /// </summary>
        /// <remarks>
        /// Sphere-Sphere collision only generates one contact.
        /// </remarks>
        public readonly Vector2[] Contacts;

        /// <summary>
        /// Constructs a new instance of <see cref="CollisionFeatures"/>.
        /// </summary>
        public CollisionFeatures(bool collided, Vector2 normal, float penetration, Vector2[] contacts)
        {
            Collided = collided;
            Normal = normal;
            Penetration = penetration;
            Contacts = contacts;
        }
    }
}
