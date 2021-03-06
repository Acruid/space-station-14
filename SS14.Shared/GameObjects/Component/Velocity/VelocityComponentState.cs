using System;

namespace SS14.Shared.GameObjects.Components.Velocity
{
    [Serializable]
    public class VelocityComponentState : ComponentState
    {
        public float VelocityX;
        public float VelocityY;

        public VelocityComponentState(float velx, float vely)
            : base(NetIDs.VELOCITY)
        {
            VelocityX = velx;
            VelocityY = vely;
        }
    }
}
