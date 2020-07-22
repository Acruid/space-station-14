using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Robust.Client.Physics
{
    [UsedImplicitly]
    public class PhysicsSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private TimeSpan _lastRem;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            _lastRem = _gameTiming.CurTime;
            _physicsManager.SimulateWorlds(TimeSpan.FromSeconds(frameTime), false);
        }

        /// <inheritdoc />
        public override void FrameUpdate(float frameTime)
        {
            if (_lastRem > _gameTiming.TickRemainder)
            {
                _lastRem = TimeSpan.Zero;
            }

            var diff = _gameTiming.TickRemainder - _lastRem;
            _lastRem = _gameTiming.TickRemainder;
            float frameTime1 = (float) diff.TotalSeconds;
            _physicsManager.SimulateWorlds(TimeSpan.FromSeconds(frameTime1), true);
        }
    }
}
