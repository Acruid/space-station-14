using System;
using System.Text;
using JetBrains.Annotations;
using Lidgren.Network;

namespace Robust.Shared.Utility
{
    /// <summary>
    /// Writes data values to a stream of bits.
    /// </summary>
    [PublicAPI]
    public class BitWriter : BitStream
    {
        /// <summary>
        /// Writes a boolean value using 1 bit
        /// </summary>
        public void Write(bool value)
        {
            EnsureBufferSize(BitLength + 1);
            NetBitWriter.WriteByte((value ? (byte)1 : (byte)0), 1, Data, BitLength);
            BitLength += 1;
        }

        /// <summary>
        /// Write a byte
        /// </summary>
        public void Write(byte source)
        {
            EnsureBufferSize(BitLength + 8);
            NetBitWriter.WriteByte(source, 8, Data, BitLength);
            BitLength += 8;
        }

        /// <summary>
        /// Writes a signed byte
        /// </summary>
        public void Write(sbyte source)
        {
            EnsureBufferSize(BitLength + 8);
            NetBitWriter.WriteByte((byte)source, 8, Data, BitLength);
            BitLength += 8;
        }

        /// <summary>
        /// Writes 1 to 8 bits of a byte
        /// </summary>
        public void Write(byte source, int numberOfBits)
        {
            NetException.Assert((numberOfBits > 0 && numberOfBits <= 8), "Write(byte, numberOfBits) can only write between 1 and 8 bits");
            EnsureBufferSize(BitLength + numberOfBits);
            NetBitWriter.WriteByte(source, numberOfBits, Data, BitLength);
            BitLength += numberOfBits;
        }

        /// <summary>
        /// Writes all bytes in an array
        /// </summary>
        public void Write(byte[] source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            var bits = source.Length * 8;
            EnsureBufferSize(BitLength + bits);
            NetBitWriter.WriteBytes(source, 0, source.Length, Data, BitLength);
            BitLength += bits;
        }

        /// <summary>
        /// Writes the specified number of bytes from an array
        /// </summary>
        public void Write(byte[] source, int offsetInBytes, int numberOfBytes)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            var bits = numberOfBytes * 8;
            EnsureBufferSize(BitLength + bits);
            NetBitWriter.WriteBytes(source, offsetInBytes, numberOfBytes, Data, BitLength);
            BitLength += bits;
        }

        /// <summary>
        /// Writes an unsigned 16 bit integer
        /// </summary>
        /// <param name="source"></param>
        public void Write(ushort source)
        {
            EnsureBufferSize(BitLength + 16);
            NetBitWriter.WriteUInt16(source, 16, Data, BitLength);
            BitLength += 16;
        }

        /// <summary>
        /// Writes an unsigned integer using 1 to 16 bits
        /// </summary>
        public void Write(ushort source, int numberOfBits)
        {
            NetException.Assert((numberOfBits > 0 && numberOfBits <= 16), "Write(ushort, numberOfBits) can only write between 1 and 16 bits");
            EnsureBufferSize(BitLength + numberOfBits);
            NetBitWriter.WriteUInt16(source, numberOfBits, Data, BitLength);
            BitLength += numberOfBits;
        }

        /// <summary>
        /// Writes a signed 16 bit integer
        /// </summary>
        public void Write(short source)
        {
            EnsureBufferSize(BitLength + 16);
            NetBitWriter.WriteUInt16((ushort)source, 16, Data, BitLength);
            BitLength += 16;
        }

        /// <summary>
        /// Writes a 32 bit signed integer
        /// </summary>
        public void Write(int source)
        {
            EnsureBufferSize(BitLength + 32);
            NetBitWriter.WriteUInt32((uint)source, 32, Data, BitLength);
            BitLength += 32;
        }

        /// <summary>
        /// Writes a 32 bit unsigned integer
        /// </summary>
        public void Write(uint source)
        {
            EnsureBufferSize(BitLength + 32);
            NetBitWriter.WriteUInt32(source, 32, Data, BitLength);
            BitLength += 32;
        }

        /// <summary>
        /// Writes a 32 bit signed integer
        /// </summary>
        public void Write(uint source, int numberOfBits)
        {
            NetException.Assert((numberOfBits > 0 && numberOfBits <= 32), "Write(uint, numberOfBits) can only write between 1 and 32 bits");
            EnsureBufferSize(BitLength + numberOfBits);
            NetBitWriter.WriteUInt32(source, numberOfBits, Data, BitLength);
            BitLength += numberOfBits;
        }

        /// <summary>
        /// Writes a signed integer using 1 to 32 bits
        /// </summary>
        public void Write(int source, int numberOfBits)
        {
            NetException.Assert((numberOfBits > 0 && numberOfBits <= 32), "Write(int, numberOfBits) can only write between 1 and 32 bits");
            EnsureBufferSize(BitLength + numberOfBits);

            if (numberOfBits != 32)
            {
                // make first bit sign
                var signBit = 1 << (numberOfBits - 1);
                if (source < 0)
                    source = (-source - 1) | signBit;
                else
                    source &= (~signBit);
            }

            NetBitWriter.WriteUInt32((uint)source, numberOfBits, Data, BitLength);

            BitLength += numberOfBits;
        }

        /// <summary>
        /// Writes a 64 bit unsigned integer
        /// </summary>
        public void Write(ulong source)
        {
            EnsureBufferSize(BitLength + 64);
            NetBitWriter.WriteUInt64(source, 64, Data, BitLength);
            BitLength += 64;
        }

        /// <summary>
        /// Writes an unsigned integer using 1 to 64 bits
        /// </summary>
        public void Write(ulong source, int numberOfBits)
        {
            EnsureBufferSize(BitLength + numberOfBits);
            NetBitWriter.WriteUInt64(source, numberOfBits, Data, BitLength);
            BitLength += numberOfBits;
        }

        /// <summary>
        /// Writes a 64 bit signed integer
        /// </summary>
        public void Write(long source)
        {
            EnsureBufferSize(BitLength + 64);
            var usource = (ulong)source;
            NetBitWriter.WriteUInt64(usource, 64, Data, BitLength);
            BitLength += 64;
        }

        /// <summary>
        /// Writes a signed integer using 1 to 64 bits
        /// </summary>
        public void Write(long source, int numberOfBits)
        {
            EnsureBufferSize(BitLength + numberOfBits);
            var usource = (ulong)source;
            NetBitWriter.WriteUInt64(usource, numberOfBits, Data, BitLength);
            BitLength += numberOfBits;
        }

        /// <summary>
        /// Writes a 32 bit floating point value
        /// </summary>
        public void Write(float source)
        {
            // Use union to avoid BitConverter.GetBytes() which allocates memory on the heap
            SingleUIntUnion su;
            su.UIntValue = 0; // must initialize every member of the union to avoid warning
            su.SingleValue = source;
            Write(su.UIntValue);
        }

        /// <summary>
        /// Writes a 64 bit floating point value
        /// </summary>
        public void Write(double source)
        {
            var val = BitConverter.GetBytes(source);
            Write(val);
        }

        //
        // Variable bits
        //

        /// <summary>
        /// Write Base128 encoded variable sized unsigned integer of up to 32 bits
        /// </summary>
        /// <returns>number of bytes written</returns>
        public int WriteVariableUInt32(uint value)
        {
            var retval = 1;
            var num1 = value;
            while (num1 >= 0x80)
            {
                this.Write((byte)(num1 | 0x80));
                num1 >>= 7;
                retval++;
            }
            this.Write((byte)num1);
            return retval;
        }

        /// <summary>
        /// Write Base128 encoded variable sized signed integer of up to 32 bits
        /// </summary>
        /// <returns>number of bytes written</returns>
        public int WriteVariableInt32(int value)
        {
            var zigzag = (uint)(value << 1) ^ (uint)(value >> 31);
            return WriteVariableUInt32(zigzag);
        }

        /// <summary>
        /// Write Base128 encoded variable sized signed integer of up to 64 bits
        /// </summary>
        /// <returns>number of bytes written</returns>
        public int WriteVariableInt64(long value)
        {
            var zigzag = (ulong)(value << 1) ^ (ulong)(value >> 63);
            return WriteVariableUInt64(zigzag);
        }

        /// <summary>
        /// Write Base128 encoded variable sized unsigned integer of up to 64 bits
        /// </summary>
        /// <returns>number of bytes written</returns>
        public int WriteVariableUInt64(ulong value)
        {
            var retval = 1;
            var num1 = value;
            while (num1 >= 0x80)
            {
                this.Write((byte)(num1 | 0x80));
                num1 >>= 7;
                retval++;
            }
            this.Write((byte)num1);
            return retval;
        }

        /// <summary>
        /// Compress (lossy) a float in the range -1..1 using numberOfBits bits
        /// </summary>
        public void WriteSignedSingle(float value, int numberOfBits)
        {
            NetException.Assert(((value >= -1.0) && (value <= 1.0)), " WriteSignedSingle() must be passed a float in the range -1 to 1; val is " + value);

            var unit = (value + 1.0f) * 0.5f;
            var maxVal = (1 << numberOfBits) - 1;
            var writeVal = (uint)(unit * maxVal);

            Write(writeVal, numberOfBits);
        }

        /// <summary>
        /// Compress (lossy) a float in the range 0..1 using numberOfBits bits
        /// </summary>
        public void WriteUnitSingle(float value, int numberOfBits)
        {
            NetException.Assert(((value >= 0.0) && (value <= 1.0)), " WriteUnitSingle() must be passed a float in the range 0 to 1; val is " + value);

            var maxValue = (1 << numberOfBits) - 1;
            var writeVal = (uint)(value * maxValue);

            Write(writeVal, numberOfBits);
        }

        /// <summary>
        /// Compress a float within a specified range using a certain number of bits
        /// </summary>
        public void WriteRangedSingle(float value, float min, float max, int numberOfBits)
        {
            NetException.Assert(((value >= min) && (value <= max)), " WriteRangedSingle() must be passed a float in the range MIN to MAX; val is " + value);

            var range = max - min;
            var unit = ((value - min) / range);
            var maxVal = (1 << numberOfBits) - 1;
            Write((uint)(maxVal * unit), numberOfBits);
        }

        /// <summary>
        /// Writes an integer with the least amount of bits need for the specified range
        /// Returns number of bits written
        /// </summary>
        public int WriteRangedInteger(int min, int max, int value)
        {
            NetException.Assert(value >= min && value <= max, "Value not within min/max range!");

            var range = (uint)(max - min);
            var numBits = NetUtility.BitsToHoldUInt(range);

            var rvalue = (uint)(value - min);
            Write(rvalue, numBits);

            return numBits;
        }

        /// <summary>
        /// Write a string
        /// </summary>
        public void Write(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                EnsureBufferSize(BitLength + 8);
                WriteVariableUInt32(0);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(source);
            EnsureBufferSize(BitLength + 8 + (bytes.Length * 8));
            WriteVariableUInt32((uint)bytes.Length);
            Write(bytes);
        }
    }
}
