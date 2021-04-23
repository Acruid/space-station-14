using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    internal sealed class ClientOccluderComponent : OccluderComponent
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        
        [ViewVariables] private (GridId, Vector2i) _lastPosition;
        [ViewVariables] internal OccluderDir Occluding { get; private set; }
        [ViewVariables] internal uint UpdateGeneration { get; set; }

        public override bool Enabled
        {
            get => base.Enabled;
            set
            {
                base.Enabled = value;

                SendDirty();
            }
        }

        protected override void Startup()
        {
            base.Startup();

            if (Owner.HasComponent<SnapGridComponent>())
            {
                SnapGridOnPositionChanged();
            }
        }

        public void SnapGridOnPositionChanged()
        {
            SendDirty();

            if(!Owner.HasComponent<SnapGridComponent>())
                return;

            var grid = _mapManager.GetGrid(Owner.Transform.GridID);
            _lastPosition = (Owner.Transform.GridID, grid.SnapGridCellFor(Owner.Transform.Coordinates));
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            SendDirty();
        }

        private void SendDirty()
        {
            if (Owner.HasComponent<SnapGridComponent>())
            {
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local,
                    new OccluderDirtyEvent(Owner, _lastPosition));
            }
        }

        internal void Update()
        {
            Occluding = OccluderDir.None;

            if (Deleted || !Owner.TryGetComponent<SnapGridComponent>(out var snapGrid))
            {
                return;
            }

            void CheckDir(Direction dir, OccluderDir oclDir)
            {
                foreach (var neighbor in MapGrid.GetInDir(snapGrid, dir))
                {
                    if (neighbor.TryGetComponent(out ClientOccluderComponent? comp) && comp.Enabled)
                    {
                        Occluding |= oclDir;
                        break;
                    }
                }
            }

            CheckDir(Direction.North, OccluderDir.North);
            CheckDir(Direction.East, OccluderDir.East);
            CheckDir(Direction.South, OccluderDir.South);
            CheckDir(Direction.West, OccluderDir.West);
        }

        [Flags]
        internal enum OccluderDir : byte
        {
            None = 0,
            North = 1,
            East = 1 << 1,
            South = 1 << 2,
            West = 1 << 3,
        }
    }
}
