// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.Graphics.PackedVector.PackUtils
// Assembly: Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553
// MVID: 34D977AE-C3EC-4D07-AA6D-6FED6D8E3864
// Assembly location: C:\Windows\Microsoft.NET\assembly\GAC_32\Microsoft.Xna.Framework\v4.0_4.0.0.0__842cf8be1de50553\Microsoft.Xna.Framework.dll

using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector
{
    internal static class PackUtils
    {
        public static uint PackUnsigned(float bitmask, float value)
        {
            return (uint)PackUtils.ClampAndRound(value, 0.0f, bitmask);
        }

        public static uint PackSigned(uint bitmask, float value)
        {
            float max = (float)(bitmask >> 1);
            float min = (float)(-(double)max - 1.0);
            return (uint)(int)PackUtils.ClampAndRound(value, min, max) & bitmask;
        }

        public static uint PackUNorm(float bitmask, float value)
        {
            value *= bitmask;
            return (uint)PackUtils.ClampAndRound(value, 0.0f, bitmask);
        }

        public static float UnpackUNorm(uint bitmask, uint value)
        {
            value &= bitmask;
            return (float)value / (float)bitmask;
        }

        public static uint PackSNorm(uint bitmask, float value)
        {
            float max = (float)(bitmask >> 1);
            value *= max;
            return (uint)(int)PackUtils.ClampAndRound(value, -max, max) & bitmask;
        }

        public static float UnpackSNorm(uint bitmask, uint value)
        {
            uint num1 = bitmask + 1U >> 1;
            if (((int)value & (int)num1) != 0)
            {
                if (((int)value & (int)bitmask) == (int)num1)
                    return -1f;
                value |= ~bitmask;
            }
            else
                value &= bitmask;
            float num2 = (float)(bitmask >> 1);
            return (float)(int)value / num2;
        }

        private static double ClampAndRound(float value, float min, float max)
        {
            if (float.IsNaN(value))
                return 0.0;
            if (float.IsInfinity(value))
                return float.IsNegativeInfinity(value) ? (double)min : (double)max;
            if ((double)value < (double)min)
                return (double)min;
            if ((double)value > (double)max)
                return (double)max;
            return Math.Round((double)value);
        }
    }
}
