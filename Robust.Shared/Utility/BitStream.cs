using System;
using JetBrains.Annotations;

namespace Robust.Shared.Utility
{
    /// <summary>
    /// Treats an in-memory array of bytes as a stream of bits.
    /// </summary>
    [PublicAPI]
    public abstract class BitStream
    {
        /// <summary>
        /// Number of bytes to over-allocate for each message to avoid resizing
        /// </summary>
        private const int OverAllocateAmount = 4;

        protected byte[] Data;
        protected int BitLength;
        protected int ReadPosition;

        /// <summary>
        /// Gets or sets the internal data buffer
        /// </summary>
        public byte[] BackingStorage
        {
            get => Data;
            set => Data = value;
        }

        /// <summary>
        /// Gets or sets the length of the used portion of the buffer in bytes
        /// </summary>
        public int LengthBytes
        {
            get => (BitLength + 7) >> 3;
            set
            {
                BitLength = value * 8;
                InternalEnsureBufferSize(BitLength);
            }
        }

        /// <summary>
        /// Gets or sets the length of the used portion of the buffer in bits
        /// </summary>
        public int LengthBits
        {
            get => BitLength;
            set
            {
                BitLength = value;
                InternalEnsureBufferSize(BitLength);
            }
        }

        /// <summary>
        /// Gets or sets the read position in the buffer, in bits (not bytes)
        /// </summary>
        public long Position
        {
            get => ReadPosition;
            set => ReadPosition = (int)value;
        }

        /// <summary>
        /// Gets the position in the buffer in bytes; note that the bits of the first returned byte may already have been read - check the Position property to make sure.
        /// </summary>
        public int PositionInBytes => ReadPosition / 8;

        /// <summary>
        /// Ensures the buffer can hold this number of bits
        /// </summary>
        public void EnsureBufferSize(int numberOfBits)
        {
            var byteLen = ((numberOfBits + 7) >> 3);
            if (Data == null)
            {
                Data = new byte[byteLen + OverAllocateAmount];
                return;
            }

            if (Data.Length < byteLen)
                Array.Resize(ref Data, byteLen + OverAllocateAmount);
        }

        /// <summary>
        /// Ensures the buffer can hold this number of bits
        /// </summary>
        protected void InternalEnsureBufferSize(int numberOfBits)
        {
            var byteLen = ((numberOfBits + 7) >> 3);
            if (Data == null)
            {
                Data = new byte[byteLen];
                return;
            }

            if (Data.Length < byteLen)
                Array.Resize(ref Data, byteLen);
        }
    }
}
