using System;

namespace SS14.Shared.GameObjects.Components.Direction
{
    [Serializable]
    public class DirectionComponentState : ComponentState
    {
        public Maths.Direction Direction;

        public DirectionComponentState(Maths.Direction dir)
            :base(ComponentFamily.Direction)
        {
            Direction = dir;
        }
    }
}
