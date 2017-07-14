using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Utility;

namespace SS14.Shared.GameObjects.Components.Transform
{
    /// <summary>
    /// This holds the transformations of the entity in world space.
    /// </summary>
    public class TransformComponent : Component
    {
        private const float Epsilon = 0.0001f;

        private bool _firstState = true;

        private Vector2 _position;
        private float _rotation;
        private float _scale;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TransformComponent()
        {
            Family = ComponentFamily.Transform;

            _position = Vector2.Zero;
            _rotation = 0.0f;
            _scale = 1.0f;
        }

        /// <summary>
        /// The network state type to sync this component.
        /// </summary>
        public override Type StateType => typeof(TransformComponentState);

        /// <inheritdoc />
        public override string Name => "Transform";

        /// <summary>
        /// The Y coordinate of the current position in world space.
        /// </summary>
        public float X
        {
            get => _position.X;
            set => Position = new Vector2(value, _position.Y);
        }

        /// <summary>
        /// The Y coordinate of the current position in world space.
        /// </summary>
        public float Y
        {
            get => _position.Y;
            set => Position = new Vector2(_position.X, value);
        }

        /// <summary>
        /// The current position in world space.
        /// </summary>
        public Vector2 Position
        {
            get => _position;
            set => SetPosition(ref value);
        }

        /// <summary>
        /// Sets the position in world space.
        /// </summary>
        /// <param name="pos"></param>
        public void SetPosition(ref Vector2 pos)
        {
            if(pos == _position)
                return;

            var oldPosition = _position;
            _position = pos;

            OnMove?.Invoke(this, new VectorEventArgs(oldPosition.Convert(), _position.Convert()));
        }

        /// <summary>
        /// Offsets the current world position by this vector.
        /// </summary>
        /// <param name="vec">The offset vector.</param>
        public void OffsetPosition(ref Vector2 vec)
        {
            if(vec.LengthSquared < Epsilon)
                return;

            Position = _position + vec;
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            var state = new TransformComponentState(_position.X, _position.Y, _firstState);
            _firstState = false;
            return state;
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            _position = Vector2.Zero;
            _rotation = 0.0f;
            _scale = 1.0f;
        }

        // all of this needs to go somewhere else, or be removed
        #region Clientside Stuff

        public event EventHandler<VectorEventArgs> OnMove;

        private readonly List<TransformComponentState> states = new List<TransformComponentState>();
        private TransformComponentState lastState;
        public TransformComponentState lerpStateFrom;
        public TransformComponentState lerpStateTo;

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            if (IoCManager.Resolve<INetManager>().IsServer)
                return default(ComponentReplyMessage);

            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return reply;

            if (type == ComponentMessageType.Initialize)
            {
                SendComponentInstantiationMessage();
            }

            return reply;
        }

        public override void OnAdd(IEntity owner)
        {
            if (IoCManager.Resolve<INetManager>().IsServer)
                return;

            base.OnAdd(owner);
            if (Owner.Initialized)
            {
                SendComponentInstantiationMessage();
            }
        }

        /// <summary>
        /// Client message to server saying component has been instantiated and needs initial data
        /// </summary>
        [Obsolete("Getting rid of this messaging paradigm.")]
        public void SendComponentInstantiationMessage()
        {
            if (IoCManager.Resolve<INetManager>().IsServer)
                return;

            var manager = IoCManager.Resolve<IEntityNetworkManager>();
            manager.SendEntityNetworkMessage(
                Owner,
                EntityMessage.ComponentInstantiationMessage,
                Family);
        }

        public override void HandleComponentState(dynamic state)
        {
            if (IoCManager.Resolve<INetManager>().IsServer)
                return;

            SetNewState((TransformComponentState) state);
        }

        private void SetNewState(TransformComponentState state)
        {
            if (IoCManager.Resolve<INetManager>().IsServer)
                return;

            lastState = state;
            states.Add(state);
            var interp = IoCManager.Resolve<IConfigurationManager>().GetCVar<float>("net.interpolation");
            //Remove all states older than the one just before the interp time.
            lerpStateFrom = states.Where(s => s.ReceivedTime <= state.ReceivedTime - interp).OrderByDescending(s => s.ReceivedTime).FirstOrDefault();
            if (lerpStateFrom != null)
            {
                lerpStateTo =
                    states.Where(s => s.ReceivedTime > lerpStateFrom.ReceivedTime).OrderByDescending(s => s.ReceivedTime).LastOrDefault();
                if (lerpStateTo == null)
                    lerpStateTo = lerpStateFrom;
                states.RemoveAll(s => s.ReceivedTime < lerpStateFrom.ReceivedTime);
            }
            else
            {
                lerpStateFrom = state;
                lerpStateTo = state;
            }
            if (lastState.ForceUpdate)
                Position = new Vector2(state.X, state.Y);
        }

        #endregion
    }
}
