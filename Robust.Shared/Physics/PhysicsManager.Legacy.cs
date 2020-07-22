﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <inheritdoc />
    public partial class PhysicsManager : IPhysicsManager
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly ConcurrentDictionary<MapId, DynamicBroadPhase> _treesPerMap =
            new ConcurrentDictionary<MapId, DynamicBroadPhase>();

        private DynamicBroadPhase this[MapId mapId] => _treesPerMap.GetOrAdd(mapId, _ => new DynamicBroadPhase());

        /// <summary>
        ///     returns true if collider intersects a physBody under management.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <param name="map">Map ID to filter</param>
        /// <returns></returns>
        public bool TryCollideRect(Box2 collider, MapId map)
        {
            foreach (var body in this[map].Query(collider))
            {
                if (!body.CanCollide || body.CollisionLayer == 0x0)
                    continue;

                if (body.MapID == map &&
                    body.WorldAABB.Intersects(collider))
                    return true;
            }

            return false;
        }

        public bool IsWeightless(GridCoordinates gridPosition)
        {
            var tile = _mapManager.GetGrid(gridPosition.GridID).GetTileRef(gridPosition).Tile;
            return !_mapManager.GetGrid(gridPosition.GridID).HasGravity || tile.IsEmpty;
        }

        // Impulse resolution algorithm based on Box2D's approach in combination with Randy Gaul's Impulse Engine resolution algorithm.

        public IEnumerable<IEntity> GetCollidingEntities(IPhysBody physBody, Vector2 offset, bool approximate = true)
        {
            var modifiers = physBody.Owner.GetAllComponents<ICollideSpecial>();
            foreach ( var body in this[physBody.MapID].Query(physBody.WorldAABB, approximate))
            {
                if (body.Owner.Deleted) {
                    continue;
                }

                if (Manifold.CollidesOnMask(physBody, body))
                {
                    var preventCollision = false;
                    var otherModifiers = body.Owner.GetAllComponents<ICollideSpecial>();
                    foreach (var modifier in modifiers)
                    {
                        preventCollision |= modifier.PreventCollide(body);
                    }
                    foreach (var modifier in otherModifiers)
                    {
                        preventCollision |= modifier.PreventCollide(physBody);
                    }

                    if (preventCollision) continue;
                    yield return body.Owner;
                }
            }
        }

        public bool IsColliding(IPhysBody body, Vector2 offset, bool approximate)
        {
            return GetCollidingEntities(body, offset, approximate).Any();
        }

        /// <summary>
        ///     Adds a physBody to the manager.
        /// </summary>
        /// <param name="physBody"></param>
        public void AddBody(IPhysBody physBody)
        {
            if (!_worlds.TryGetValue(physBody.MapID, out var world))
            {
                world = new PhysWorld(this);
                _worlds.Add(physBody.MapID, world);
            }
            world.AddBody(physBody);

            if (!this[physBody.MapID].Add(physBody))
            {
                Logger.WarningS("phys", $"PhysicsBody already registered! {physBody.Owner}");
            }
        }

        /// <summary>
        ///     Removes a physBody from the manager
        /// </summary>
        /// <param name="physBody"></param>
        public void RemoveBody(IPhysBody physBody)
        {
            _worlds[physBody.MapID].RemoveBody(physBody);

            var removed = false;

            if (physBody.Owner.Deleted || physBody.Owner.Transform.Deleted)
            {
                foreach (var mapId in _mapManager.GetAllMapIds())
                {
                    removed = this[mapId].Remove(physBody);

                    if (removed)
                    {
                        break;
                    }
                }
            }

            if (!removed)
            {
                try
                {
                    removed = this[physBody.MapID].Remove(physBody);
                }
                catch (InvalidOperationException)
                {
                    // TODO: TryGetMapId or something
                    foreach (var mapId in _mapManager.GetAllMapIds())
                    {
                        removed = this[mapId].Remove(physBody);

                        if (removed)
                        {
                            break;
                        }
                    }
                }
            }

            if (!removed)
            {
                foreach (var mapId in _mapManager.GetAllMapIds())
                {
                    removed = this[mapId].Remove(physBody);

                    if (removed)
                    {
                        break;
                    }
                }
            }

            if (!removed)
                Logger.WarningS("phys", $"Trying to remove unregistered PhysicsBody! {physBody.Owner.Uid}");
        }

        /// <inheritdoc />
        public IEnumerable<RayCastResults> IntersectRayWithPredicate(MapId mapId, CollisionRay ray,
            float maxLength = 50F,
            Func<IEntity, bool>? predicate = null, bool returnOnFirstHit = true)
        {
            List<RayCastResults> results = new List<RayCastResults>();

            this[mapId].Query((ref IPhysBody body, in Vector2 point, float distFromOrigin) =>
            {

                if (returnOnFirstHit && results.Count > 0) return true;

                if (distFromOrigin > maxLength)
                {
                    return true;
                }

                if (!body.CanCollide)
                {
                    return true;
                }

                if ((body.CollisionLayer & ray.CollisionMask) == 0x0)
                {
                    return true;
                }

                if (predicate != null && predicate.Invoke(body.Owner))
                {
                    return true;
                }

                var result = new RayCastResults(distFromOrigin, point, body.Owner);
                results.Add(result);
                DebugDrawRay?.Invoke(new DebugRayData(ray, maxLength, result));
                return true;
            }, ray.Position, ray.Direction);
            if (results.Count == 0)
            {
                DebugDrawRay?.Invoke(new DebugRayData(ray, maxLength, null));
            }

            results.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return results;
        }

        /// <inheritdoc />
        public IEnumerable<RayCastResults> IntersectRay(MapId mapId, CollisionRay ray, float maxLength = 50, IEntity? ignoredEnt = null, bool returnOnFirstHit = true)
            => IntersectRayWithPredicate(mapId, ray, maxLength, entity => entity == ignoredEnt, returnOnFirstHit);

        /// <inheritdoc />
        public float IntersectRayPenetration(MapId mapId, CollisionRay ray, float maxLength, IEntity? ignoredEnt = null)
        {
            var penetration = 0f;

            this[mapId].Query((ref IPhysBody body, in Vector2 point, float distFromOrigin) =>
            {
                if (distFromOrigin > maxLength)
                {
                    return true;
                }

                if (!body.CanCollide)
                {
                    return true;
                }

                if ((body.CollisionLayer & ray.CollisionMask) == 0x0)
                {
                    return true;
                }

                if (new Ray(point + ray.Direction * body.WorldAABB.Size.Length * 2, -ray.Direction).Intersects(
                    body.WorldAABB, out _, out var exitPoint))
                {
                    penetration += (point - exitPoint).Length;
                }
                return true;
            }, ray.Position, ray.Direction);

            return penetration;
        }

        public event Action<DebugRayData>? DebugDrawRay;

        public bool Update(IPhysBody collider)
        {
            collider.WakeBody();

            if (!_worlds.TryGetValue(collider.MapID, out var world))
            {
                world = new PhysWorld(this);
                _worlds.Add(collider.MapID, world);
            }
            _worlds[collider.MapID].WakeBody(collider);

            return this[collider.MapID].Update(collider);
        }

        public void RemovedFromMap(IPhysBody body, MapId mapId)
        {
            body.WakeBody();
            _worlds[mapId].RemoveBody(body);
            this[mapId].Remove(body);
        }

        public void AddedToMap(IPhysBody body, MapId mapId)
        {
            body.WakeBody();
            _worlds[mapId].AddBody(body);
            this[mapId].Add(body);
        }
    }
}
