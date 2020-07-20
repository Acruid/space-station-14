﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components
{
    public class CollidableComponent : Component, ICollidableComponent
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        private bool _canCollide;
        private bool _isHard;
        private BodyStatus _status;
        private BodyType _bodyType;
        private List<IPhysShape> _physShapes = new List<IPhysShape>();

        /// <inheritdoc />
        public override string Name => "Collidable";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.COLLIDABLE;

        /// <inheritdoc />
        public MapId MapID => Owner.Transform.MapID;

        /// <inheritdoc />
        public int ProxyId { get; set; }

        /// <inheritdoc />
        public BodyType BodyType { get; set; } = BodyType.Static;

        /// <inheritdoc />
        public int SleepAccumulator { get; set; }

        public int SleepThreshold
        {
            get => _physicsManager.SleepTimeThreshold;
            set => _physicsManager.SleepTimeThreshold = value;
        }

        /// <inheritdoc />
        public bool Awake => _physicsManager.SleepTimeThreshold > SleepAccumulator;

        /// <inheritdoc />
        public void WakeBody()
        {
            SleepAccumulator = 0;
        }

        public bool IsDynamic([NotNullWhen(true)] out IPhysicsComponent? physicsComp)
        {
            if (Owner.TryGetComponent(out physicsComp) && BodyType == BodyType.Dynamic)
            {
                return true;
            }

            physicsComp = default;
            return false;
        }

        public bool IsDynamic()
        {
            return Owner.HasComponent<IPhysicsComponent>() && BodyType == BodyType.Dynamic;
        }

        public IPhysicsComponent PhysicsComponent { get; private set; }

        public bool SetupPhysicsProxy()
        {
            if (Owner.TryGetComponent<IPhysicsComponent>(out var physicsComponent))
            {
                PhysicsComponent = physicsComponent;
                return true;
            }

            return false;
        }

        public Vector2 Position
        {
            get => Owner.Transform.WorldPosition;
            set => Owner.Transform.WorldPosition = value;
        }

        public float Rotation
        {
            get => (float) Owner.Transform.WorldRotation.Theta;
            set => Owner.Transform.WorldRotation = new Angle(value);
        }

        public Vector2 LinearVelocity
        {
            get => PhysicsComponent.LinearVelocity;
            set => PhysicsComponent.LinearVelocity = value;
        }

        public float AngularVelocity
        {
            get => PhysicsComponent.AngularVelocity;
            set => PhysicsComponent.AngularVelocity = value;
        }

        private Vector2 _force;
        public Vector2 Force
        {
            get => _force;
            set => _force = value;
        }

        private float _torque;
        public float Torque
        {
            get => _torque;
            set => _torque = value;
        }

        public float Mass
        {
            get => PhysicsComponent.Mass;
            set => PhysicsComponent.Mass = value;
        }
        public float I { get; set; }

        public float InvMass
        {
            get => PhysicsComponent.Mass > 0 ? 1f / PhysicsComponent.Mass : 0f;
            set => PhysicsComponent.Mass = value > 0 ? 1f / value : 0f;
        }

        public float InvI { get; set; }

        public CollidableComponent()
        {
            PhysicsShapes = new PhysShapeList(this);
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _canCollide, "on", true);
            serializer.DataField(ref _isHard, "hard", true);
            serializer.DataField(ref _status, "Status", BodyStatus.OnGround);
            serializer.DataField(ref _bodyType, "bodyType", BodyType.Static);
            serializer.DataField(ref _physShapes, "shapes", new List<IPhysShape>{new PhysShapeAabb()});
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new CollidableComponentState(_canCollide, _status, _physShapes, _isHard);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState == null)
                return;

            var newState = (CollidableComponentState)curState;

            _canCollide = newState.CanCollide;
            _status = newState.Status;
            _isHard = newState.Hard;

            //TODO: Is this always true?
            if (newState.PhysShapes != null)
            {
                _physShapes = newState.PhysShapes;

                foreach (var shape in _physShapes)
                {
                    shape.ApplyState();
                }

                Dirty();
                UpdateEntityTree();
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        Box2 IPhysBody.WorldAABB
        {
            get
            {
                var pos = Owner.Transform.WorldPosition;
                return ((IPhysBody)this).AABB.Translated(pos);
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        Box2 IPhysBody.AABB
        {
            get
            {
                var angle = Owner.Transform.WorldRotation;
                var bounds = new Box2();

                foreach (var shape in _physShapes)
                {
                    var shapeBounds = shape.CalculateLocalBounds(angle);
                    bounds = bounds.IsEmpty() ? shapeBounds : bounds.Union(shapeBounds);
                }

                return bounds;
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public IList<IPhysShape> PhysicsShapes { get; }

        /// <summary>
        ///     Enables or disabled collision processing of this component.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanCollide
        {
            get => _canCollide;
            set
            {
                _canCollide = value;
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new CollisionChangeMessage(Owner.Uid, _canCollide));
                Dirty();
            }
        }

        /// <summary>
        ///     Non-hard collidables will not cause action collision (e.g. blocking of movement)
        ///     while still raising collision events.
        /// </summary>
        /// <remarks>
        ///     This is useful for triggers or such to detect collision without actually causing a blockage.
        /// </remarks>
        [ViewVariables]
        public bool Hard
        {
            get => _isHard;
            set
            {
                _isHard = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Bitmask of the collision layers this component is a part of.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionLayer
        {
            get
            {
                var layers = 0x0;
                foreach (var shape in _physShapes)
                    layers = layers | shape.CollisionLayer;
                return layers;
            }
        }

        /// <summary>
        ///     Bitmask of the layers this component collides with.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionMask
        {
            get
            {
                var mask = 0x0;
                foreach (var shape in _physShapes)
                    mask = mask | shape.CollisionMask;
                return mask;
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public BodyStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                Dirty();
            }
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            // normally ExposeData would create this
            if (_physShapes == null)
            {
                _physShapes = new List<IPhysShape> { new PhysShapeAabb() };
            }
            else
            {
                foreach (var shape in _physShapes)
                {
                    ShapeAdded(shape);
                }
            }

            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new CollisionChangeMessage(Owner.Uid, _canCollide));
        }

        public override void OnRemove()
        {
            base.OnRemove();

            // In case somebody starts sharing shapes across multiple components I guess?
            foreach (var shape in _physShapes)
            {
                ShapeRemoved(shape);
            }
            
            // Should we not call this if !_canCollide? PathfindingSystem doesn't care at least.
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new CollisionChangeMessage(Owner.Uid, false));
        }



        private void ShapeAdded(IPhysShape shape)
        {
            shape.OnDataChanged += ShapeDataChanged;
        }

        private void ShapeRemoved(IPhysShape item)
        {
            item.OnDataChanged -= ShapeDataChanged;
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();
            _physicsManager.AddBody(this);
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            _physicsManager.RemoveBody(this);
            base.Shutdown();
        }

        public bool IsColliding(Vector2 offset, bool approx = true)
        {
            return _physicsManager.IsColliding(this, offset, approx);
        }

        public IEnumerable<IEntity> GetCollidingEntities(Vector2 offset, bool approx = true)
        {
            return _physicsManager.GetCollidingEntities(this, offset, approx);
        }

        public bool UpdatePhysicsTree()
            => _physicsManager.Update(this);

        public void RemovedFromPhysicsTree(MapId mapId)
        {
            _physicsManager.RemovedFromMap(this, mapId);
        }

        public void AddedToPhysicsTree(MapId mapId)
        {
            _physicsManager.AddedToMap(this, mapId);
        }

        private bool UpdateEntityTree() => Owner.EntityManager.UpdateEntityTree(Owner);

        public bool IsOnGround()
        {
            return Status == BodyStatus.OnGround;
        }

        public bool IsInAir()
        {
            return Status == BodyStatus.InAir;
        }

        private void ShapeDataChanged()
        {
            Dirty();
            UpdatePhysicsTree();
        }

        // Custom IList<> implementation so that we can hook addition/removal of shapes.
        // To hook into their OnDataChanged event correctly.
        private sealed class PhysShapeList : IList<IPhysShape>
        {
            private readonly CollidableComponent _owner;

            public PhysShapeList(CollidableComponent owner)
            {
                _owner = owner;
            }

            public IEnumerator<IPhysShape> GetEnumerator()
            {
                return _owner._physShapes.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(IPhysShape item)
            {
                _owner._physShapes.Add(item);

                ItemAdded(item);
            }

            public void Clear()
            {
                foreach (var item in _owner._physShapes)
                {
                    ItemRemoved(item);
                }

                _owner._physShapes.Clear();
            }

            public bool Contains(IPhysShape item)
            {
                return _owner._physShapes.Contains(item);
            }

            public void CopyTo(IPhysShape[] array, int arrayIndex)
            {
                _owner._physShapes.CopyTo(array, arrayIndex);
            }

            public bool Remove(IPhysShape item)
            {
                var found = _owner._physShapes.Remove(item);
                if (found)
                {
                    ItemRemoved(item);
                }

                return found;
            }

            public int Count => _owner._physShapes.Count;
            public bool IsReadOnly => false;

            public int IndexOf(IPhysShape item)
            {
                return _owner._physShapes.IndexOf(item);
            }

            public void Insert(int index, IPhysShape item)
            {
                _owner._physShapes.Insert(index, item);
                ItemAdded(item);
            }

            public void RemoveAt(int index)
            {
                var item = _owner._physShapes[index];
                ItemRemoved(item);

                _owner._physShapes.RemoveAt(index);
            }

            public IPhysShape this[int index]
            {
                get => _owner._physShapes[index];
                set
                {
                    var oldItem = _owner._physShapes[index];
                    ItemRemoved(oldItem);

                    _owner._physShapes[index] = value;
                    ItemAdded(value);
                }
            }

            private void ItemAdded(IPhysShape item)
            {
                _owner.ShapeAdded(item);
            }

            public void ItemRemoved(IPhysShape item)
            {
                _owner.ShapeRemoved(item);
            }
        }
    }
}
