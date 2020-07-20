using System;
using System.Collections.Generic;
using Robust.Shared.Log;
using Robust.Shared.Map;

namespace Robust.Shared.Physics
{
    public partial class PhysicsManager
    {
        private readonly Dictionary<MapId, PhysWorld> _worlds = new Dictionary<MapId, PhysWorld>();

        public int SleepTimeThreshold { get; set; } = 240;

        public void AddWorld(MapId mapId)
        {
            if (_worlds.ContainsKey(mapId))
            {
                throw new InvalidOperationException($"Tried to add a new world, but world {mapId} already exists.");
            }

            _worlds.Add(mapId, new PhysWorld());
            Logger.DebugS("phys", $"Creating new PhysWorld: {mapId}");
        }

        public void RemoveWorld(MapId mapId)
        {
            _worlds.Remove(mapId);
            Logger.DebugS("phys", $"removed PhysWorld: {mapId}");
        }

        public void SimulateWorld(TimeSpan deltaTime)
        {
            //TODO: This can be ran in parallel, worlds do not interact
            foreach (var kvWorld in _worlds)
            {
                if(kvWorld.Key == MapId.Nullspace)
                    continue;

                kvWorld.Value.SimulateWorld(deltaTime);
            }
        }
    }
}
