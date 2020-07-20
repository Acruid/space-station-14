using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    internal class PhysWorld
    {
        private const float VelocityEpsilon = 1f / 128;

        private readonly IBroadPhase _broadPhase = new DynamicBroadPhase();
        private readonly HashSet<IPhysBody> _awakeBodies = new HashSet<IPhysBody>();

        public bool AddBody(IPhysBody physBody)
        {
            _awakeBodies.Add(physBody);
            return _broadPhase.Add(physBody);
        }

        public bool RemoveBody(IPhysBody body)
        {
            _awakeBodies.Remove(body);
            return _broadPhase.Remove(body);
        }

        public void WakeBody(IPhysBody body)
        {
            _awakeBodies.Add(body);
        }

        public void SimulateWorld(TimeSpan deltaTime)
        {
            var dt = (float) deltaTime.TotalSeconds;

            foreach (var body in _awakeBodies)
            {
                if(body.BodyType != BodyType.Dynamic)
                    continue;

                body.SetupPhysicsProxy();

                body.PhysicsComponent.Controller?.UpdateBeforeProcessing();

                //Integrate forces
                body.LinearVelocity += body.Force * body.InvMass * dt;
                body.AngularVelocity += body.Torque * body.InvI * dt;
            }

            var manifolds = _awakeBodies
                .SelectMany(body => FindCollisions(_broadPhase, body))
                //TODO: Is it cheaper to remove {(A,B), (B,A)} duplicates here,
                // or just not care (if the first one gets resolved, second one won't make it through NarrowPhase)
                .Where(tuple => PhysicsManager.CollidesOnMask(tuple.Item1, tuple.Item2)) //TODO: Make BroadPhase do this way earlier
                .Where((tuple => ShouldCollideCallback(tuple.Item1, tuple.Item2)))
                .Select(tuple => new Manifold(tuple.Item1, tuple.Item2))
                .ToList(); // allows awakeBodies to be modified

            //Note that collision resolution will modify the set of awake bodies
            foreach (var manifold in manifolds)
            {
                //TODO: Resolve collisions
            }

            // Integrate Velocities (including newly awakened bodies)
            foreach (var body in _awakeBodies)
            {
                // non-dynamic bodies don't move
                if (body.BodyType != BodyType.Dynamic)
                {
                    body.SleepAccumulator++;
                    continue;
                }

                var lVel = body.LinearVelocity * dt;
                var aVel = body.AngularVelocity * dt;

                if (lVel.Length < VelocityEpsilon && aVel < VelocityEpsilon)
                {
                    body.SleepAccumulator++;
                    body.LinearVelocity = Vector2.Zero;
                    body.AngularVelocity = 0;
                }
                else
                {
                    body.SleepAccumulator = 0;
                    body.Position += lVel;
                    body.Rotation += aVel;
                }

                body.Force = Vector2.Zero;
                body.Torque = 0f;

                body.PhysicsComponent.Controller?.UpdateAfterProcessing();
            }

            _awakeBodies.RemoveWhere(body => !body.Awake);
        }

        private static IEnumerable<(IPhysBody, IPhysBody)> FindCollisions(IBroadPhase broadPhase, IPhysBody body)
        {
            // Queries the BroadPhase for any bodies that collide with the given body.

            return broadPhase.Query(body.WorldAABB).Select(collidedBody => (body, collidedBody));
        }

        private static bool ShouldCollideCallback(IPhysBody left, IPhysBody right)
        {
            // Take note here, this is the BroadPhase "should these two bodies ever collide". This is not
            // "these two bodies have collided". The two bodies in question may not *actually* be colliding
            // at this point. These callback functions are going to be called *a lot*, they need to be
            // very lightweight. This callback is not for bullet collisions.

            //TODO: 99% of entities are not going to use this feature, consider setting a boolean on
            // the body for if we should even attempt the callback

            // Notice the early exits, if the first callback blocks collision, there is no point of
            // continuing to check the next callbacks to see if collision should be blocked.

            foreach (var callback in left.Owner.GetAllComponents<ICollideSpecial>())
            {
                if (callback.PreventCollide(right))
                    return false;
            }

            foreach (var callback in right.Owner.GetAllComponents<ICollideSpecial>())
            {
                if (callback.PreventCollide(left))
                    return false;
            }

            return true;
        }

        private readonly struct Manifold
        {
            public readonly IPhysBody Left;
            public readonly IPhysBody Right;

            public Manifold(IPhysBody left, IPhysBody right)
            {
                Left = left;
                Right = right;
            }
        }
    }
}
