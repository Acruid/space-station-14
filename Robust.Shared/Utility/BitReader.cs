using System;
using System.Text;
using JetBrains.Annotations;
using Lidgren.Network;

namespace Robust.Shared.Utility
{
    /// <summary>
    /// Reads data values from a stream of bits.
    /// </summary>
    [PublicAPI]
    public class BitReader : BitStream
    {
        private const string ReadOverflowError = "Trying to read past the buffer size - likely caused by mismatching Write/Reads, different size or order.";

        #region Read

        /// <summary>
        /// Reads a boolean value (stored as a single bit) written using Write(bool)
        /// </summary>
        public bool ReadBoolean()
        {
            DebugTools.Assert(BitLength - ReadPosition >= 1, ReadOverflowError);
            var retval = NetBitWriter.ReadByte(Data, 1, ReadPosition);
            ReadPosition += 1;
            return retval > 0;
        }

        /// <summary>
        /// Reads a byte
        /// </summary>
        public byte ReadByte()
        {
            DebugTools.Assert(BitLength - ReadPosition >= 8, ReadOverflowError);
            var retval = NetBitWriter.ReadByte(Data, 8, ReadPosition);
            ReadPosition += 8;
            return retval;
        }

        /// <summary>
        /// Reads a signed byte
        /// </summary>
        public sbyte ReadSByte()
        {
            NetException.Assert(BitLength - ReadPosition >= 8, ReadOverflowError);
            var retval = NetBitWriter.ReadByte(Data, 8, ReadPosition);
            ReadPosition += 8;
            return (sbyte)retval;
        }

        /// <summary>
        /// Reads 1 to 8 bits into a byte
        /// </summary>
        public byte ReadByte(int numberOfBits)
        {
            NetException.Assert(numberOfBits > 0 && numberOfBits <= 8, "ReadByte(bits) can only read between 1 and 8 bits");
            var retval = NetBitWriter.ReadByte(Data, numberOfBits, ReadPosition);
            ReadPosition += numberOfBits;
            return retval;
        }

        /// <summary>
        /// Reads the specified number of bytes
        /// </summary>
        public byte[] ReadBytes(int numberOfBytes)
        {
            NetException.Assert(BitLength - ReadPosition + 7 >= (numberOfBytes * 8), ReadOverflowError);

            var retval = new byte[numberOfBytes];
            NetBitWriter.ReadBytes(Data, numberOfBytes, ReadPosition, retval, 0);
            ReadPosition += (8 * numberOfBytes);
            return retval;
        }
        /// <summary>
        /// Reads the specified number of bytes into a pre-allocated array
        /// </summary>
        /// <param name="into">The destination array</param>
        /// <param name="offset">The offset where to start writing in the destination array</param>
        /// <param name="numberOfBytes">The number of bytes to read</param>
        public void ReadBytes(byte[] into, int offset, int numberOfBytes)
        {
            NetException.Assert(BitLength - ReadPosition + 7 >= (numberOfBytes * 8), ReadOverflowError);
            NetException.Assert(offset + numberOfBytes <= into.Length);

            NetBitWriter.ReadBytes(Data, numberOfBytes, ReadPosition, into, offset);
            ReadPosition += (8 * numberOfBytes);
        }

        /// <summary>
        /// Reads the specified number of bits into a pre-allocated array
        /// </summary>
        /// <param name="into">The destination array</param>
        /// <param name="offset">The offset where to start writing in the destination array</param>
        /// <param name="numberOfBits">The number of bits to read</param>
        public void ReadBits(byte[] into, int offset, int numberOfBits)
        {
            NetException.Assert(BitLength - ReadPosition >= numberOfBits, ReadOverflowError);
            NetException.Assert(offset + NetUtility.BytesToHoldBits(numberOfBits) <= into.Length);

            var numberOfWholeBytes = numberOfBits / 8;
            var extraBits = numberOfBits - (numberOfWholeBytes * 8);

            NetBitWriter.ReadBytes(Data, numberOfWholeBytes, ReadPosition, into, offset);
            ReadPosition += (8 * numberOfWholeBytes);

            if (extraBits > 0)
                into[offset + numberOfWholeBytes] = ReadByte(extraBits);
        }

        /// <summary>
        /// Reads a 16 bit signed integer written using Write(Int16)
        /// </summary>
        public short ReadInt16()
        {
            NetException.Assert(BitLength - ReadPosition >= 16, ReadOverflowError);
            uint retval = NetBitWriter.ReadUInt16(Data, 16, ReadPosition);
            ReadPosition += 16;
            return (short)retval;
        }

        /// <summary>
        /// Reads a 16 bit unsigned integer written using Write(UInt16)
        /// </summary>
        public ushort ReadUInt16()
        {
            NetException.Assert(BitLength - ReadPosition >= 16, ReadOverflowError);
            uint retval = NetBitWriter.ReadUInt16(Data, 16, ReadPosition);
            ReadPosition += 16;
            return (ushort)retval;
        }

        /// <summary>
        /// Reads a 32 bit signed integer written using Write(Int32)
        /// </summary>
        public int ReadInt32()
        {
            NetException.Assert(BitLength - ReadPosition >= 32, ReadOverflowError);
            var retval = NetBitWriter.ReadUInt32(Data, 32, ReadPosition);
            ReadPosition += 32;
            return (int)retval;
        }

        /// <summary>
        /// Reads a signed integer stored in 1 to 32 bits, written using Write(Int32, Int32)
        /// </summary>
        public int ReadInt32(int numberOfBits)
        {
            NetException.Assert(numberOfBits > 0 && numberOfBits <= 32, "ReadInt32(bits) can only read between 1 and 32 bits");
            NetException.Assert(BitLength - ReadPosition >= numberOfBits, ReadOverflowError);

            var retval = NetBitWriter.ReadUInt32(Data, numberOfBits, ReadPosition);
            ReadPosition += numberOfBits;

            if (numberOfBits == 32)
                return (int)retval;

            var signBit = 1 << (numberOfBits - 1);
            if ((retval & signBit) == 0)
                return (int)retval; // positive

            // negative
            unchecked
            {
                var mask = ((uint)-1) >> (33 - numberOfBits);
                var tmp = (retval & mask) + 1;
                return -((int)tmp);
            }
        }

        /// <summary>
        /// Reads an 32 bit unsigned integer written using Write(UInt32)
        /// </summary>
        public uint ReadUInt32()
        {
            NetException.Assert(BitLength - ReadPosition >= 32, ReadOverflowError);
            var retval = NetBitWriter.ReadUInt32(Data, 32, ReadPosition);
            ReadPosition += 32;
            return retval;
        }

        /// <summary>
        /// Reads an unsigned integer stored in 1 to 32 bits, written using Write(UInt32, Int32)
        /// </summary>
        public uint ReadUInt32(int numberOfBits)
        {
            NetException.Assert(numberOfBits > 0 && numberOfBits <= 32, "ReadUInt32(bits) can only read between 1 and 32 bits");

            var retval = NetBitWriter.ReadUInt32(Data, numberOfBits, ReadPosition);
            ReadPosition += numberOfBits;
            return retval;
        }

        /// <summary>
        /// Reads a 64 bit unsigned integer written using Write(UInt64)
        /// </summary>
        public ulong ReadUInt64()
        {
            NetException.Assert(BitLength - ReadPosition >= 64, ReadOverflowError);

            ulong low = NetBitWriter.ReadUInt32(Data, 32, ReadPosition);
            ReadPosition += 32;
            ulong high = NetBitWriter.ReadUInt32(Data, 32, ReadPosition);

            var retval = low + (high << 32);

            ReadPosition += 32;
            return retval;
        }

        /// <summary>
        /// Reads a 64 bit signed integer written using Write(Int64)
        /// </summary>
        public long ReadInt64()
        {
            NetException.Assert(BitLength - ReadPosition >= 64, ReadOverflowError);
            unchecked
            {
                var retval = ReadUInt64();
                var longRetval = (long)retval;
                return longRetval;
            }
        }

        /// <summary>
        /// Reads an unsigned integer stored in 1 to 64 bits, written using Write(UInt64, Int32)
        /// </summary>
        public ulong ReadUInt64(int numberOfBits)
        {
            NetException.Assert(numberOfBits > 0 && numberOfBits <= 64, "ReadUInt64(bits) can only read between 1 and 64 bits");
            NetException.Assert(BitLength - ReadPosition >= numberOfBits, ReadOverflowError);

            ulong retval;
            if (numberOfBits <= 32)
            {
                retval = NetBitWriter.ReadUInt32(Data, numberOfBits, ReadPosition);
            }
            else
            {
                retval = NetBitWriter.ReadUInt32(Data, 32, ReadPosition);
                retval |= NetBitWriter.ReadUInt32(Data, numberOfBits - 32, ReadPosition) << 32;
            }
            ReadPosition += numberOfBits;
            return retval;
        }

        /// <summary>
        /// Reads a signed integer stored in 1 to 64 bits, written using Write(Int64, Int32)
        /// </summary>
        public long ReadInt64(int numberOfBits)
        {
            NetException.Assert(((numberOfBits > 0) && (numberOfBits <= 64)), "ReadInt64(bits) can only read between 1 and 64 bits");
            return (long)ReadUInt64(numberOfBits);
        }

        /// <summary>
        /// Reads a 32 bit floating point value written using Write(Single)
        /// </summary>
        public float ReadFloat()
        {
            return ReadSingle();
        }

        /// <summary>
        /// Reads a 32 bit floating point value written using Write(Single)
        /// </summary>
        public float ReadSingle()
        {
            NetException.Assert(BitLength - ReadPosition >= 32, ReadOverflowError);

            if ((ReadPosition & 7) == 0) // read directly
            {
                var retval = BitConverter.ToSingle(Data, ReadPosition >> 3);
                ReadPosition += 32;
                return retval;
            }

            var bytes = ReadBytes(4);
            return BitConverter.ToSingle(bytes, 0);
        }


        /// <summary>
        /// Reads a 64 bit floating point value written using Write(Double)
        /// </summary>
        public double ReadDouble()
        {
            NetException.Assert(BitLength - ReadPosition >= 64, ReadOverflowError);

            if ((ReadPosition & 7) == 0) // read directly
            {
                // read directly
                var retval = BitConverter.ToDouble(Data, ReadPosition >> 3);
                ReadPosition += 64;
                return retval;
            }

            var bytes = ReadBytes(8);
            return BitConverter.ToDouble(bytes, 0);
        }

        //
        // Variable bit count
        //

        /// <summary>
        /// Reads a variable sized UInt32 written using WriteVariableUInt32()
        /// </summary>
        public uint ReadVariableUInt32()
        {
            var num1 = 0;
            var num2 = 0;
            while (true)
            {
                var num3 = ReadByte();
                num1 |= (num3 & 0x7f) << num2;
                num2 += 7;
                if ((num3 & 0x80) == 0)
                    return (uint)num1;
            }
        }
        /// <summary>
        /// Reads a variable sized Int32 written using WriteVariableInt32()
        /// </summary>
        public int ReadVariableInt32()
        {
            var n = ReadVariableUInt32();
            return (int)(n >> 1) ^ -(int)(n & 1); // decode zigzag
        }

        /// <summary>
        /// Reads a variable sized Int64 written using WriteVariableInt64()
        /// </summary>
        public long ReadVariableInt64()
        {
            var n = ReadVariableUInt64();
            return (long)(n >> 1) ^ -(long)(n & 1); // decode zigzag
        }

        /// <summary>
        /// Reads a variable sized UInt32 written using WriteVariableInt64()
        /// </summary>
        public ulong ReadVariableUInt64()
        {
            ulong num1 = 0;
            var num2 = 0;
            while (true)
            {
                var num3 = ReadByte();
                num1 |= ((ulong)num3 & 0x7f) << num2;
                num2 += 7;
                if ((num3 & 0x80) == 0)
                    return num1;
            }
        }

        /// <summary>
        /// Reads a 32 bit floating point value written using WriteSignedSingle()
        /// </summary>
        /// <param name="numberOfBits">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to -1 and smaller or equal to 1</returns>
        public float ReadSignedSingle(int numberOfBits)
        {
            var encodedVal = ReadUInt32(numberOfBits);
            var maxVal = (1 << numberOfBits) - 1;
            return ((encodedVal + 1) / (float)(maxVal + 1) - 0.5f) * 2.0f;
        }

        /// <summary>
        /// Reads a 32 bit floating point value written using WriteUnitSingle()
        /// </summary>
        /// <param name="numberOfBits">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to 0 and smaller or equal to 1</returns>
        public float ReadUnitSingle(int numberOfBits)
        {
            var encodedVal = ReadUInt32(numberOfBits);
            var maxVal = (1 << numberOfBits) - 1;
            return (encodedVal + 1) / (float)(maxVal + 1);
        }

        /// <summary>
        /// Reads a 32 bit floating point value written using WriteRangedSingle()
        /// </summary>
        /// <param name="min">The minimum value used when writing the value</param>
        /// <param name="max">The maximum value used when writing the value</param>
        /// <param name="numberOfBits">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to MIN and smaller or equal to MAX</returns>
        public float ReadRangedSingle(float min, float max, int numberOfBits)
        {
            var range = max - min;
            var maxVal = (1 << numberOfBits) - 1;
            var encodedVal = (float)ReadUInt32(numberOfBits);
            var unit = encodedVal / maxVal;
            return min + (unit * range);
        }

        /// <summary>
        /// Reads a 32 bit integer value written using WriteRangedInteger()
        /// </summary>
        /// <param name="min">The minimum value used when writing the value</param>
        /// <param name="max">The maximum value used when writing the value</param>
        /// <returns>A signed integer value larger or equal to MIN and smaller or equal to MAX</returns>
        public int ReadRangedInteger(int min, int max)
        {
            var range = (uint)(max - min);
            var numBits = NetUtility.BitsToHoldUInt(range);

            var rvalue = ReadUInt32(numBits);
            return (int)(min + rvalue);
        }

        /// <summary>
        /// Reads a string written using Write(string)
        /// </summary>
        public string ReadString()
        {
            var byteLen = (int)ReadVariableUInt32();

            if (byteLen == 0)
                return string.Empty;

            NetException.Assert(BitLength - ReadPosition >= (byteLen * 8), ReadOverflowError);

            if ((ReadPosition & 7) == 0)
            {
                // read directly
                var retval = Encoding.UTF8.GetString(Data, ReadPosition >> 3, byteLen);
                ReadPosition += (8 * byteLen);
                return retval;
            }

            var bytes = ReadBytes(byteLen);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        #endregion

        #region Peek

        //
        // 1 bit
        //
        /// <summary>
        /// Reads a 1-bit Boolean without advancing the read pointer
        /// </summary>
        public bool PeekBoolean()
        {
            NetException.Assert(BitLength - ReadPosition >= 1, ReadOverflowError);
            byte retval = NetBitWriter.ReadByte(Data, 1, ReadPosition);
            return retval > 0;
        }

        //
        // 8 bit 
        //
        /// <summary>
        /// Reads a Byte without advancing the read pointer
        /// </summary>
        public byte PeekByte()
        {
            NetException.Assert(BitLength - ReadPosition >= 8, ReadOverflowError);
            byte retval = NetBitWriter.ReadByte(Data, 8, ReadPosition);
            return retval;
        }

        /// <summary>
        /// Reads an SByte without advancing the read pointer
        /// </summary>
        public sbyte PeekSByte()
        {
            NetException.Assert(BitLength - ReadPosition >= 8, ReadOverflowError);
            byte retval = NetBitWriter.ReadByte(Data, 8, ReadPosition);
            return (sbyte)retval;
        }

        /// <summary>
        /// Reads the specified number of bits into a Byte without advancing the read pointer
        /// </summary>
        public byte PeekByte(int numberOfBits)
        {
            byte retval = NetBitWriter.ReadByte(Data, numberOfBits, ReadPosition);
            return retval;
        }

        /// <summary>
        /// Reads the specified number of bytes without advancing the read pointer
        /// </summary>
        public byte[] PeekBytes(int numberOfBytes)
        {
            NetException.Assert(BitLength - ReadPosition >= (numberOfBytes * 8), ReadOverflowError);

            byte[] retval = new byte[numberOfBytes];
            NetBitWriter.ReadBytes(Data, numberOfBytes, ReadPosition, retval, 0);
            return retval;
        }

        /// <summary>
        /// Reads the specified number of bytes without advancing the read pointer
        /// </summary>
        public void PeekBytes(byte[] into, int offset, int numberOfBytes)
        {
            NetException.Assert(BitLength - ReadPosition >= (numberOfBytes * 8), ReadOverflowError);
            NetException.Assert(offset + numberOfBytes <= into.Length);

            NetBitWriter.ReadBytes(Data, numberOfBytes, ReadPosition, into, offset);
        }

        //
        // 16 bit
        //
        /// <summary>
        /// Reads an Int16 without advancing the read pointer
        /// </summary>
        public short PeekInt16()
        {
            NetException.Assert(BitLength - ReadPosition >= 16, ReadOverflowError);
            uint retval = NetBitWriter.ReadUInt16(Data, 16, ReadPosition);
            return (short)retval;
        }

        /// <summary>
        /// Reads a UInt16 without advancing the read pointer
        /// </summary>
        public ushort PeekUInt16()
        {
            NetException.Assert(BitLength - ReadPosition >= 16, ReadOverflowError);
            uint retval = NetBitWriter.ReadUInt16(Data, 16, ReadPosition);
            return (ushort)retval;
        }

        //
        // 32 bit
        //
        /// <summary>
        /// Reads an Int32 without advancing the read pointer
        /// </summary>
        public int PeekInt32()
        {
            NetException.Assert(BitLength - ReadPosition >= 32, ReadOverflowError);
            uint retval = NetBitWriter.ReadUInt32(Data, 32, ReadPosition);
            return (int)retval;
        }

        /// <summary>
        /// Reads the specified number of bits into an Int32 without advancing the read pointer
        /// </summary>
        public int PeekInt32(int numberOfBits)
        {
            NetException.Assert((numberOfBits > 0 && numberOfBits <= 32), "ReadInt() can only read between 1 and 32 bits");
            NetException.Assert(BitLength - ReadPosition >= numberOfBits, ReadOverflowError);

            uint retval = NetBitWriter.ReadUInt32(Data, numberOfBits, ReadPosition);

            if (numberOfBits == 32)
                return (int)retval;

            int signBit = 1 << (numberOfBits - 1);
            if ((retval & signBit) == 0)
                return (int)retval; // positive

            // negative
            unchecked
            {
                uint mask = ((uint)-1) >> (33 - numberOfBits);
                uint tmp = (retval & mask) + 1;
                return -((int)tmp);
            }
        }

        /// <summary>
        /// Reads a UInt32 without advancing the read pointer
        /// </summary>
        public uint PeekUInt32()
        {
            NetException.Assert(BitLength - ReadPosition >= 32, ReadOverflowError);
            uint retval = NetBitWriter.ReadUInt32(Data, 32, ReadPosition);
            return retval;
        }

        /// <summary>
        /// Reads the specified number of bits into a UInt32 without advancing the read pointer
        /// </summary>
        public uint PeekUInt32(int numberOfBits)
        {
            NetException.Assert((numberOfBits > 0 && numberOfBits <= 32), "ReadUInt() can only read between 1 and 32 bits");

            uint retval = NetBitWriter.ReadUInt32(Data, numberOfBits, ReadPosition);
            return retval;
        }

        //
        // 64 bit
        //
        /// <summary>
        /// Reads a UInt64 without advancing the read pointer
        /// </summary>
        public ulong PeekUInt64()
        {
            NetException.Assert(BitLength - ReadPosition >= 64, ReadOverflowError);

            ulong low = NetBitWriter.ReadUInt32(Data, 32, ReadPosition);
            ulong high = NetBitWriter.ReadUInt32(Data, 32, ReadPosition + 32);

            ulong retval = low + (high << 32);

            return retval;
        }

        /// <summary>
        /// Reads an Int64 without advancing the read pointer
        /// </summary>
        public long PeekInt64()
        {
            NetException.Assert(BitLength - ReadPosition >= 64, ReadOverflowError);
            unchecked
            {
                ulong retval = PeekUInt64();
                long longRetval = (long)retval;
                return longRetval;
            }
        }

        /// <summary>
        /// Reads the specified number of bits into an UInt64 without advancing the read pointer
        /// </summary>
        public ulong PeekUInt64(int numberOfBits)
        {
            NetException.Assert((numberOfBits > 0 && numberOfBits <= 64), "ReadUInt() can only read between 1 and 64 bits");
            NetException.Assert(BitLength - ReadPosition >= numberOfBits, ReadOverflowError);

            ulong retval;
            if (numberOfBits <= 32)
            {
                retval = NetBitWriter.ReadUInt32(Data, numberOfBits, ReadPosition);
            }
            else
            {
                retval = NetBitWriter.ReadUInt32(Data, 32, ReadPosition);
                retval |= NetBitWriter.ReadUInt32(Data, numberOfBits - 32, ReadPosition) << 32;
            }
            return retval;
        }

        /// <summary>
        /// Reads the specified number of bits into an Int64 without advancing the read pointer
        /// </summary>
        public long PeekInt64(int numberOfBits)
        {
            NetException.Assert(((numberOfBits > 0) && (numberOfBits < 65)), "ReadInt64(bits) can only read between 1 and 64 bits");
            return (long)PeekUInt64(numberOfBits);
        }

        //
        // Floating point
        //
        /// <summary>
        /// Reads a 32-bit Single without advancing the read pointer
        /// </summary>
        public float PeekFloat()
        {
            return PeekSingle();
        }

        /// <summary>
        /// Reads a 32-bit Single without advancing the read pointer
        /// </summary>
        public float PeekSingle()
        {
            NetException.Assert(BitLength - ReadPosition >= 32, ReadOverflowError);

            if ((ReadPosition & 7) == 0) // read directly
            {
                float retval = BitConverter.ToSingle(Data, ReadPosition >> 3);
                return retval;
            }

            byte[] bytes = PeekBytes(4);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Reads a 64-bit Double without advancing the read pointer
        /// </summary>
        public double PeekDouble()
        {
            NetException.Assert(BitLength - ReadPosition >= 64, ReadOverflowError);

            if ((ReadPosition & 7) == 0) // read directly
            {
                // read directly
                double retval = BitConverter.ToDouble(Data, ReadPosition >> 3);
                return retval;
            }

            byte[] bytes = PeekBytes(8);
            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// Reads a string without advancing the read pointer
        /// </summary>
        public string PeekString()
        {
            int wasReadPosition = ReadPosition;
            string retval = ReadString();
            ReadPosition = wasReadPosition;
            return retval;
        }

        #endregion
    }
}
