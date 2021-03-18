using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates
{
    internal class EntityViewCulling
    {
        private const int ViewSetCapacity = 128; // starting number of entities that are in view
        private const int PlayerSetSize = 64; // Starting number of players
        private const int MaxVisPoolSize = 1024; // Maximum number of pooled objects

        private readonly IServerEntityManager _entMan;
        private readonly IComponentManager _compMan;
        private readonly IMapManager _mapManager;

        private readonly Dictionary<ICommonSession, HashSet<EntityUid>> _playerVisibleSets = new(PlayerSetSize);

        private readonly List<(GameTick tick, EntityUid uid)> _deletionHistory = new();

        private readonly ObjectPool<HashSet<EntityUid>> _visSetPool
            = new DefaultObjectPool<HashSet<EntityUid>>(new DefaultPooledObjectPolicy<HashSet<EntityUid>>(), MaxVisPoolSize);

        private readonly ObjectPool<HashSet<EntityUid>> _viewerEntsPool
            = new DefaultObjectPool<HashSet<EntityUid>>(new DefaultPooledObjectPolicy<HashSet<EntityUid>>(), MaxVisPoolSize);

        /// <summary>
        /// Is view culling enabled, or will we send the whole map?
        /// </summary>
        public bool CullingEnabled { get; set; }

        /// <summary>
        /// Size of the side of the view bounds square.
        /// </summary>
        public float ViewSize { get; set; }

        public EntityViewCulling(IServerEntityManager entMan, IMapManager mapManager)
        {
            _entMan = entMan;
            _compMan = entMan.ComponentManager;
            _mapManager = mapManager;
            _compMan = _entMan.ComponentManager;
        }

        // Not thread safe
        public void EntityDeleted(EntityUid e)
        {
            // Not aware of prediction
            _deletionHistory.Add((_entMan.CurrentTick, e));
        }

        // Not thread safe
        public void CullDeletionHistory(GameTick oldestAck)
        {
            _deletionHistory.RemoveAll(hist => hist.tick < oldestAck);
        }

        private List<EntityUid>? GetDeletedEntities(GameTick fromTick)
        {
            var list = new List<EntityUid>();
            foreach (var (tick, id) in _deletionHistory)
            {
                if (tick >= fromTick) list.Add(id);
            }

            // no point sending an empty collection
            return list.Count == 0 ? default : list;
        }

        // Not thread safe
        public void AddPlayer(ICommonSession session)
        {
            _playerVisibleSets.Add(session, new HashSet<EntityUid>(ViewSetCapacity));
        }

        // Not thread safe
        public void RemovePlayer(ICommonSession session)
        {
            _playerVisibleSets.Remove(session);
        }

        // thread safe
        public bool IsPointVisible(ICommonSession session, in MapCoordinates position)
        {
            var viewables = GetSessionViewers(session);

            bool CheckInView(MapCoordinates mapCoordinates, HashSet<EntityUid> entityUids)
            {
                foreach (var euid in entityUids)
                {
                    var (viewBox, mapId) = CalcViewBounds(in euid);

                    if (mapId != mapCoordinates.MapId)
                        continue;

                    if (!CullingEnabled)
                        return true;

                    if (viewBox.Contains(mapCoordinates.Position))
                        return true;
                }

                return false;
            }

            bool result = CheckInView(position, viewables);

            viewables.Clear();
            _viewerEntsPool.Return(viewables);
            return result;
        }

        private HashSet<EntityUid> GetSessionViewers(ICommonSession session)
        {
            var viewers = _viewerEntsPool.Get();
            if (session.Status != SessionStatus.InGame || session.AttachedEntityUid is null)
                return viewers;

            var query = _compMan.EntityQuery<BasicActorComponent>();

            foreach (var actorComp in query)
            {
                if (actorComp.playerSession == session)
                    viewers.Add(actorComp.Owner.Uid);
            }

            return viewers;
        }

        // thread safe
        public (List<EntityState>? updates, List<EntityUid>? deletions) CalculateEntityStates(ICommonSession session, GameTick fromTick)
        {
            DebugTools.Assert(session.Status == SessionStatus.InGame);

            //TODO: Stop sending all entities to every player first tick
            List<EntityUid>? deletions;
            if (!CullingEnabled || fromTick == GameTick.Zero)
            {
                var allStates = _entMan.GetEntityStates(session, fromTick);
                deletions = GetDeletedEntities(fromTick);
                return (allStates, deletions);
            }

            var currentSet = CalcCurrentViewSet(session);

            // If they don't have a usable eye, nothing to send, and map remove will deal with ent removal
            if (currentSet is null)
                return (null, null);

            // pretty big allocations :(
            List<EntityState> entityStates = new(currentSet.Count);
            var previousSet = _playerVisibleSets[session];

            // complement set
            foreach (var entityUid in previousSet)
            {
                if (!currentSet.Contains(entityUid))
                {
                    //TODO: PVS Leave Message
                }
            }

            foreach (var entityUid in currentSet)
            {
                if (previousSet.Contains(entityUid))
                {
                    //Still Visible
                    // only send new changes
                    var newState = _entMan.GetEntityState(entityUid, fromTick, session);

                    if (!newState.Empty)
                        entityStates.Add(newState);
                }
                else
                {
                    // PVS enter message
                    // don't assume the client knows anything about us
                    var newState = _entMan.GetEntityState(entityUid, GameTick.Zero, session);
                    entityStates.Add(newState);
                }
            }

            // swap out vis sets
            _playerVisibleSets[session] = currentSet;
            previousSet.Clear();
            _visSetPool.Return(previousSet);

            deletions = GetDeletedEntities(fromTick);

            return (entityStates, deletions);
        }

        private HashSet<EntityUid>? CalcCurrentViewSet(ICommonSession session)
        {
            if (!CullingEnabled)
                return null;

            // if you don't have an attached entity, you don't see the world.
            if (session.AttachedEntityUid is null)
                return null;

            var visibleEnts = _visSetPool.Get();
            var viewers = GetSessionViewers(session);

            foreach (var eyeEuid in viewers)
            {
                var (viewBox, mapId) = CalcViewBounds(in eyeEuid);

                uint visMask = 0;
                if (_compMan.TryGetComponent<EyeComponent>(eyeEuid, out var eyeComp))
                    visMask = eyeComp.VisibilityMask;

                //Always include the map entity
                visibleEnts.Add(_mapManager.GetMapEntityId(mapId));

                //Always include viewable ent itself
                visibleEnts.Add(eyeEuid);

                // grid entity should be added through this
                // assume there are no deleted ents in here, cull them first in ent/comp manager
                _entMan.FastEntitiesIntersecting(in mapId, ref viewBox, entity => RecursiveAdd((TransformComponent)entity.Transform, visibleEnts, visMask));
            }

            viewers.Clear();
            _viewerEntsPool.Return(viewers);

            return visibleEnts;
        }

        // Read Safe

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private bool RecursiveAdd(TransformComponent xform, HashSet<EntityUid> visSet, uint visMask)
        {
            var xformUid = xform.Owner.Uid;

            // we are done, this ent has already been checked and is visible
            if (visSet.Contains(xformUid))
                return true;

            // if we are invisible, we are not going into the visSet, so don't worry about parents, and children are not going in
            if (_compMan.TryGetComponent<VisibilityComponent>(xformUid, out var visComp))
            {
                if ((visMask & visComp.Layer) == 0)
                    return false;
            }

            var xformParentUid = xform.ParentUid;
            
            // this is the world entity, it is always visible
            if (!xformParentUid.IsValid())
            {
                visSet.Add(xformUid);
                return true;
            }

            // parent is already in the set
            if (visSet.Contains(xformParentUid))
            {
                visSet.Add(xformUid);
                return true;
            }

            // parent was not added, so we are not either
            var xformParent = _compMan.GetComponent<TransformComponent>(xformParentUid);
            if (!RecursiveAdd(xformParent, visSet, visMask))
                return false;

            // add us
            visSet.Add(xformUid);
            return true;
        }

        // Read Safe
        private (Box2 view, MapId mapId) CalcViewBounds(in EntityUid euid)
        {
            var xform = _compMan.GetComponent<ITransformComponent>(euid);

            var view = Box2.UnitCentered.Scale(ViewSize).Translated(xform.WorldPosition);
            var map = xform.MapID;

            return (view, map);
        }
    }
}
