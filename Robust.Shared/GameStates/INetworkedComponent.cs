using Robust.Shared.Utility;

namespace Robust.Shared.GameStates
{
    public interface INetworkedComponent
    {
        public void WriteNetworkState(BitDeltaWriter state);

        public void ReadNetworkState(BitReader curState, BitReader nextState);
    }
}
