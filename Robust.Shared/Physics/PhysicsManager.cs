using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;

namespace Robust.Shared.Physics
{
    public partial class PhysicsManager
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        private readonly Dictionary<MapId, PhysWorld> _worlds = new Dictionary<MapId, PhysWorld>();

        /// <inheritdoc />
        public int SleepTimeThreshold { get; set; } = 240;

        /// <inheritdoc />
        public void AddWorld(MapId mapId)
        {
            if (_worlds.ContainsKey(mapId))
            {
                throw new InvalidOperationException($"Tried to add a new world, but world {mapId} already exists.");
            }

            _worlds.Add(mapId, new PhysWorld(this));
            Logger.DebugS("phys", $"Creating new PhysWorld: {mapId}");
        }

        /// <inheritdoc />
        public void RemoveWorld(MapId mapId)
        {
            _worlds.Remove(mapId);
            Logger.DebugS("phys", $"removed PhysWorld: {mapId}");
        }

        /// <inheritdoc />
        public void SimulateWorlds(TimeSpan deltaTime, bool predict)
        {
            //TODO: This can be ran in parallel, worlds do not interact
            foreach (var kvWorld in _worlds)
            {
                if(kvWorld.Key == MapId.Nullspace)
                    continue;

                //TODO: Don't simulate if map is paused, check pause manager

                kvWorld.Value.SimulateWorld(deltaTime, predict);
            }
        }

        /// <inheritdoc />
        public float GetTileFriction(IPhysicsComponent physics)
        {
            if (!physics.OnGround)
                return 0.0f;

            var mapPos = physics.Owner.Transform.MapPosition;

            if (!_mapManager.TryFindGridAt(mapPos, out var grid))
                return 1.0f;

            var tilePos = grid.WorldToTile(mapPos.Position);
            var tile = grid.GetTileRef(tilePos);
            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return tileDef.Friction;
        }
    }
}
