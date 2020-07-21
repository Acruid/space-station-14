using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    internal class PhysWorld
    {
        private readonly IPhysicsManager _physicsManager;

        private const float VelocityEpsilon = 1f / 128;

        private readonly IBroadPhase _broadPhase = new DynamicBroadPhase();
        private readonly HashSet<IPhysBody> _awakeBodies = new HashSet<IPhysBody>();

        public PhysWorld(IPhysicsManager physicsManager)
        {
            _physicsManager = physicsManager;
        }

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

        public void SimulateWorld(TimeSpan deltaTime, bool predict)
        {
            var dt = (float) deltaTime.TotalSeconds;

            foreach (var body in _awakeBodies)
            {

                if(body.BodyType != BodyType.Dynamic)
                    continue;

                if(!body.SetupPhysicsProxy())
                    continue;

                if(predict && !(body.PhysicsComponent.Predict))
                    continue;

                body.PhysicsComponent.Controller?.UpdateBeforeProcessing();

                //Integrate forces
                body.LinearVelocity += body.Force * body.InvMass * dt;
                body.AngularVelocity += body.Torque * body.InvI * dt;
            }

            var manifolds = _awakeBodies
                .Where(body => body.SetupPhysicsProxy() && (!predict || body.PhysicsComponent.Predict))
                .SelectMany(body => FindCollisions(_broadPhase, body))
                //TODO: Is it cheaper to remove {(A,B), (B,A)} duplicates here,
                // or just not care (if the first one gets resolved, second one won't make it through NarrowPhase)
                .Where(tuple => Interfaces.Physics.Manifold.CollidesOnMask(tuple.Item1, tuple.Item2)) //TODO: Make BroadPhase do this way earlier
                .Where((tuple => ShouldCollideCallback(tuple.Item1, tuple.Item2)))
                .Select(tuple => new Manifold(tuple.Item1, tuple.Item2))
                .ToList(); // allows awakeBodies to be modified

            //Note that collision resolution will modify the set of awake bodies
            foreach (var manifold in manifolds)
            {
                NarrowPhase(manifold);
            }

            // Integrate Velocities (including newly awakened bodies)
            foreach (var body in _awakeBodies)
            {
                if(!body.SetupPhysicsProxy())
                    continue;

                if (predict && !(body.PhysicsComponent.Predict))
                    continue;

                if (body.CollideCount > 0)
                {
                    var behaviors = body.Owner.GetAllComponents<ICollideBehavior>();

                    foreach (var behavior in behaviors)
                    {
                        behavior.PostCollide(body.CollideCount);
                    }

                    body.CollideCount = 0;
                }

                // non-dynamic bodies don't move
                if (body.BodyType != BodyType.Dynamic || body.PhysicsComponent.Anchored
                ) //TODO: Anchored should change BodyType
                {
                    body.SleepAccumulator++;
                    continue;
                }

                ProcessFriction(body.PhysicsComponent);

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

                    // TODO: Move this to player controller, it is used only for breaking out of a locker
                    if (ContainerHelpers.IsInContainer(body.Owner))
                    {
                        body.Owner.Transform.Parent!.Owner.SendMessage(body.Owner.Transform,
                            new RelayMovementEntityMessage(body.Owner));
                    }
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
            // This already checks AABB collision.
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

        private static void CollideWithCallback(Manifold manifold)
        {
            // Apply onCollide behavior
            var aBehaviors = manifold.Left.Owner.GetAllComponents<ICollideBehavior>();
            var hasBehavior = false;
            foreach (var behavior in aBehaviors)
            {
                var entity = manifold.Right.Owner;
                if (entity.Deleted) continue;
                behavior.CollideWith(entity);
                hasBehavior = true;
            }

            if(hasBehavior)
                manifold.Left.CollideCount++;

            var bBehaviors = manifold.Right.Owner.GetAllComponents<ICollideBehavior>();
            hasBehavior = false;
            foreach (var behavior in bBehaviors)
            {
                var entity = manifold.Left.Owner;
                if (entity.Deleted) continue;
                behavior.CollideWith(entity);
                hasBehavior = true;
            }

            if(hasBehavior)
                manifold.Left.CollideCount++;
        }

        private static void NarrowPhase(Manifold manifold)
        {
            // generate normal

            // generate overlap



            CollideWithCallback(manifold);
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

        // Based off of Randy Gaul's ImpulseEngine code
        private static bool FixClipping(IPhysicsManager physicsManager, List<Interfaces.Physics.Manifold> collisions, float divisions)
        {
            const float allowance = 0.05f;
            var percent = Math.Clamp(1f / divisions, 0.01f, 1f);
            var done = true;
            foreach (var collision in collisions)
            {
                if (!collision.Hard)
                {
                    continue;
                }

                var penetration = PhysicsManager.CalculatePenetration(collision.A, collision.B);
                if (penetration > allowance)
                {
                    done = false;
                    var correction = collision.Normal * Math.Abs(penetration) * percent;
                    if (collision.APhysics != null && !collision.APhysics.Anchored && !collision.APhysics.Deleted)
                        collision.APhysics.Owner.Transform.WorldPosition -= correction;
                    if (collision.BPhysics != null && !collision.BPhysics.Anchored && !collision.BPhysics.Deleted)
                        collision.BPhysics.Owner.Transform.WorldPosition += correction;
                }
            }

            return done;
        }

        public static void ResolveCollision(Interfaces.Physics.Manifold collision)
        {
            var impulse = PhysicsManager.SolveCollisionImpulse(collision);
            if (collision.APhysics != null)
            {
                collision.APhysics.Momentum -= impulse;
            }

            if (collision.BPhysics != null)
            {
                collision.BPhysics.Momentum += impulse;
            }
        }

        private void ProcessFriction(IPhysicsComponent physics)
        {
            if (physics.LinearVelocity.Length < VelocityEpsilon)
                return;

            // Calculate frictional force
            var friction = _physicsManager.GetTileFriction(physics);

            // Clamp friction because friction can't make you accelerate backwards
            friction = Math.Min(friction, physics.LinearVelocity.Length);

            // No multiplication/division by mass here since that would be redundant.
            var frictionVelocityChange = physics.LinearVelocity.Normalized * -friction;

            physics.LinearVelocity += frictionVelocityChange;
        }
    }
}
