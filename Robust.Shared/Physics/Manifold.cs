using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    internal struct Manifold
    {
        public Vector2 RelativeVelocity => B.LinearVelocity - A.LinearVelocity;

        public readonly Vector2 Normal;
        public readonly IPhysBody A;
        public readonly IPhysBody B;
        public readonly bool Hard;

        public bool Unresolved => Vector2.Dot(RelativeVelocity, Normal) < 0 && Hard;

        public Manifold(IPhysBody left, IPhysBody right)
        {
            A = left;
            B = right;
            Normal = CalculateNormal(left, right);
            Hard = (left.BodyType == BodyType.Static || left.BodyType == BodyType.Dynamic) && (right.BodyType == BodyType.Static || right.BodyType == BodyType.Dynamic);
        }

        /// <summary>
        ///     Calculates the normal vector for two colliding bodies
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Vector2 CalculateNormal(IPhysBody target, IPhysBody source)
        {
            // TODO: Convert both to space local to source and compare

            var manifold = target.WorldAABB.Intersect(source.WorldAABB);
            if (manifold.IsEmpty()) return Vector2.Zero;
            if (manifold.Height > manifold.Width)
            {
                // X is the axis of seperation
                var leftDist = source.WorldAABB.Right - target.WorldAABB.Left;
                var rightDist = target.WorldAABB.Right - source.WorldAABB.Left;
                return new Vector2(leftDist > rightDist ? 1 : -1, 0);
            }
            else
            {
                // Y is the axis of seperation
                var bottomDist = source.WorldAABB.Top - target.WorldAABB.Bottom;
                var topDist = target.WorldAABB.Top - source.WorldAABB.Bottom;
                return new Vector2(0, bottomDist > topDist ? 1 : -1);
            }
        }

        public static bool CollidesOnMask(IPhysBody a, IPhysBody b)
        {
            if (a == b)
                return false;

            if (a.BodyType == BodyType.None || b.BodyType == BodyType.None)
                return false;

            if (!a.CanCollide || !b.CanCollide)
                return false;

            if ((a.CollisionMask & b.CollisionLayer) == 0x0 &&
                (b.CollisionMask & a.CollisionLayer) == 0x0)
                return false;

            return true;
        }

        /// <summary>
        ///     Calculates the penetration depth of the axis-of-least-penetration for a
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static float CalculatePenetration(IPhysBody target, IPhysBody source)
        {
            var manifold = target.WorldAABB.Intersect(source.WorldAABB);
            if (manifold.IsEmpty()) return 0.0f;
            return manifold.Height > manifold.Width ? manifold.Width : manifold.Height;
        }

        public Vector2 SolveCollisionImpulse()
        {
            var aP = A.PhysicsComponent;
            var bP = B.PhysicsComponent;
            if (aP == null && bP == null) return Vector2.Zero;
            var restitution = 0.01f;
            var normal = Manifold.CalculateNormal(A, B);
            var rV = aP != null
                ? bP != null ? bP.LinearVelocity - aP.LinearVelocity : -aP.LinearVelocity
                : bP!.LinearVelocity;

            var vAlongNormal = Vector2.Dot(rV, normal);
            if (vAlongNormal > 0)
            {
                return Vector2.Zero;
            }

            var impulse = -(1.0f + restitution) * vAlongNormal;
            // So why the 100.0f instead of 0.0f? Well, because the other object needs to have SOME mass value,
            // or otherwise the physics object can actually sink in slightly to the physics-less object.
            // (the 100.0f is equivalent to a mass of 0.01kg)
            impulse /= (aP != null && aP.Mass > 0.0f ? 1 / aP.Mass : 100.0f) +
                       (bP != null && bP.Mass > 0.0f ? 1 / bP.Mass : 100.0f);
            return Normal * impulse;
        }
    }
}
