namespace Robust.Shared.Utility
{
    public class BitDeltaWriter
    {
        private BitReader _baseState;
        private BitWriter _newBaseState;
        private BitWriter _deltaBaseState;

        public bool Modified { get; private set; }

        public BitDeltaWriter(BitReader baseState, BitWriter newBaseState, BitWriter deltaBaseState)
        {
            _baseState = baseState;
            _newBaseState = newBaseState;
            _deltaBaseState = deltaBaseState;
        }

        public void Write(bool value)
        {
            _newBaseState?.Write(value);

            if (_baseState == null)
            {
                _deltaBaseState.Write(value);
                Modified = true;
            }
            else
            {
                if (_baseState.ReadBoolean().Equals(value))
                {
                    _deltaBaseState.Write(false);
                }
                else
                {
                    _deltaBaseState.Write(true);
                    _deltaBaseState.Write(value);
                    Modified = true;
                }
            }
        }

        public void Write(byte value)
        {

        }

        public void Write(sbyte value)
        {

        }

        public void Write(ushort value)
        {

        }

        public void Write(short value)
        {

        }

        public void Write(uint value)
        {

        }

        public void Write(int value)
        {

        }

        public void Write(float value)
        {

        }

        public void Write(string value)
        {

        }
    }
}
