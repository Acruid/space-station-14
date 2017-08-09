﻿using Lidgren.Network;
using SFML.System;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Utility;

namespace SS14.Client.GameObjects
{
    //Moves an entity based on key binding input
    public class PlayerInputMoverComponent : ClientComponent, IMoverComponent
    {
        /// <inheritdoc />
        public override string Name => "PlayerInputMover";

        /// <inheritdoc />
        public override uint? NetID => null;
        //public override uint? NetID => NetIDs.PLAYER_INPUT_MOVER;

        /// <inheritdoc />
        public override bool NetworkSynchronizeExistence => false;
        //public override bool NetworkSynchronizeExistence => true;

        //private const float BaseMoveSpeed = Constants.HumanWalkSpeed;
        //public const float FastMoveSpeed = Constants.HumanRunSpeed;
       // private const float MoveRateLimit = .06666f; // 15 movements allowed to be sent to the server per second.

#if _DELME
        private float _currentMoveSpeed = BaseMoveSpeed;

        private bool _moveDown;
        private bool _moveLeft;
        private bool _moveRight;
        private float _moveTimeCache;
        private bool _moveUp;
        public bool ShouldSendPositionUpdate;

        private Vector2f Velocity
        {
            get => Owner.GetComponent<PhysicsComponent>().Velocity.Convert();
            set => Owner.GetComponent<PhysicsComponent>().Velocity = value.Convert();
        }

        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.ReceiveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;
            switch (type)
            {
                case ComponentMessageType.BoundKeyChange:
                    HandleKeyChange(list);
                    break;
            }
            return reply;
        }

        /// <summary>
        /// Handles a changed keystate message
        /// </summary>
        /// <param name="list">0 - Function, 1 - Key State</param>
        private void HandleKeyChange(params object[] list)
        {
            var function = (BoundKeyFunctions)list[0];
            var state = (BoundKeyState)list[1];
            bool setting = state == BoundKeyState.Down;

            ShouldSendPositionUpdate = true;
            /*if (state == BoundKeyState.Up)
                SendPositionUpdate(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);*/
            // Send a position update so that the server knows what position the client ended at.

            if (function == BoundKeyFunctions.MoveDown)
                _moveDown = setting;
            if (function == BoundKeyFunctions.MoveUp)
                _moveUp = setting;
            if (function == BoundKeyFunctions.MoveLeft)
                _moveLeft = setting;
            if (function == BoundKeyFunctions.MoveRight)
                _moveRight = setting;
            if (function == BoundKeyFunctions.Run)
            {
                _currentMoveSpeed = setting ? FastMoveSpeed : BaseMoveSpeed;
            }
        }

        /// <summary>
        /// Update function. Processes currently pressed keys and does shit etc.
        /// </summary>
        /// <param name="frameTime"></param>
        public override void Update(float frameTime)
        {
            _moveTimeCache += frameTime;

            base.Update(frameTime);

            if (_moveUp && !_moveLeft && !_moveRight && !_moveDown) // Move Up
            {
                Velocity = new Vector2f(0, -1) * _currentMoveSpeed;
            }
            else if (_moveDown && !_moveLeft && !_moveRight && !_moveUp) // Move Down
            {
                Velocity = new Vector2f(0, 1) * _currentMoveSpeed;
            }
            else if (_moveLeft && !_moveRight && !_moveUp && !_moveDown) // Move Left
            {
                Velocity = new Vector2f(-1, 0) * _currentMoveSpeed;
            }
            else if (_moveRight && !_moveLeft && !_moveUp && !_moveDown) // Move Right
            {
                Velocity = new Vector2f(1, 0) * _currentMoveSpeed;
            }
            else if (_moveUp && _moveRight && !_moveLeft && !_moveDown) // Move Up & Right
            {
                Velocity = new Vector2f(0.7071f, -0.7071f) * _currentMoveSpeed;
            }
            else if (_moveUp && _moveLeft && !_moveRight && !_moveDown) // Move Up & Left
            {
                Velocity = new Vector2f(-0.7071f, -0.7071f) * _currentMoveSpeed;
            }
            else if (_moveDown && _moveRight && !_moveLeft && !_moveUp) // Move Down & Right
            {
                Velocity = new Vector2f(0.7071f, 0.7071f) * _currentMoveSpeed;
            }
            else if (_moveDown && _moveLeft && !_moveRight && !_moveUp) // Move Down & Left
            {
                Velocity = new Vector2f(-0.7071f, 0.7071f) * _currentMoveSpeed;
            }
            else
            {
                Velocity = new Vector2f(0f, 0f);
            }
        }

        public virtual void SendPositionUpdate(Vector2f nextPosition)
        {
            var physics = Owner.GetComponent<PhysicsComponent>();
            Owner.SendComponentNetworkMessage(this,
                                              NetDeliveryMethod.ReliableUnordered,
                                              nextPosition.X,
                                              nextPosition.Y,
                                              physics.Velocity.X,
                                              physics.Velocity.Y);

        }

#endif

    }
}
