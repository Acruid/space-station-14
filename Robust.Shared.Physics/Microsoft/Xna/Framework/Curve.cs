// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.Curve
// Assembly: Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553
// MVID: 34D977AE-C3EC-4D07-AA6D-6FED6D8E3864
// Assembly location: C:\Windows\Microsoft.NET\assembly\GAC_32\Microsoft.Xna.Framework\v4.0_4.0.0.0__842cf8be1de50553\Microsoft.Xna.Framework.dll

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Xna.Framework
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    [Serializable]
    public class Curve
    {
        private CurveKeyCollection keys = new CurveKeyCollection();
        private CurveLoopType preLoop;
        private CurveLoopType postLoop;

        public CurveLoopType PreLoop
        {
            get
            {
                return this.preLoop;
            }
            set
            {
                this.preLoop = value;
            }
        }

        public CurveLoopType PostLoop
        {
            get
            {
                return this.postLoop;
            }
            set
            {
                this.postLoop = value;
            }
        }

        public CurveKeyCollection Keys
        {
            get
            {
                return this.keys;
            }
        }

        public bool IsConstant
        {
            get
            {
                return this.keys.Count <= 1;
            }
        }

        public Curve Clone()
        {
            return new Curve()
            {
                preLoop = this.preLoop,
                postLoop = this.postLoop,
                keys = this.keys.Clone()
            };
        }

        public void ComputeTangent(int keyIndex, CurveTangent tangentType)
        {
            this.ComputeTangent(keyIndex, tangentType, tangentType);
        }

        public void ComputeTangent(
          int keyIndex,
          CurveTangent tangentInType,
          CurveTangent tangentOutType)
        {
            if (this.keys.Count <= keyIndex || keyIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(keyIndex));
            CurveKey key = this.Keys[keyIndex];
            double position;
            float num1 = (float)(position = (double)key.Position);
            float num2 = (float)position;
            float num3 = (float)position;
            double num4;
            float num5 = (float)(num4 = (double)key.Value);
            float num6 = (float)num4;
            float num7 = (float)num4;
            if (keyIndex > 0)
            {
                num3 = this.Keys[keyIndex - 1].Position;
                num7 = this.Keys[keyIndex - 1].Value;
            }
            if (keyIndex + 1 < this.keys.Count)
            {
                num1 = this.Keys[keyIndex + 1].Position;
                num5 = this.Keys[keyIndex + 1].Value;
            }
            switch (tangentInType)
            {
                case CurveTangent.Linear:
                    key.TangentIn = num6 - num7;
                    break;
                case CurveTangent.Smooth:
                    float num8 = num1 - num3;
                    float num9 = num5 - num7;
                    key.TangentIn = (double)Math.Abs(num9) >= 1.19209289550781E-07 ? num9 * Math.Abs(num3 - num2) / num8 : 0.0f;
                    break;
                default:
                    key.TangentIn = 0.0f;
                    break;
            }
            switch (tangentOutType)
            {
                case CurveTangent.Linear:
                    key.TangentOut = num5 - num6;
                    break;
                case CurveTangent.Smooth:
                    float num10 = num1 - num3;
                    float num11 = num5 - num7;
                    if ((double)Math.Abs(num11) < 1.19209289550781E-07)
                    {
                        key.TangentOut = 0.0f;
                        break;
                    }
                    key.TangentOut = num11 * Math.Abs(num1 - num2) / num10;
                    break;
                default:
                    key.TangentOut = 0.0f;
                    break;
            }
        }

        public void ComputeTangents(CurveTangent tangentType)
        {
            this.ComputeTangents(tangentType, tangentType);
        }

        public void ComputeTangents(CurveTangent tangentInType, CurveTangent tangentOutType)
        {
            for (int keyIndex = 0; keyIndex < this.Keys.Count; ++keyIndex)
                this.ComputeTangent(keyIndex, tangentInType, tangentOutType);
        }

        public float Evaluate(float position)
        {
            if (this.keys.Count == 0)
                return 0.0f;
            if (this.keys.Count == 1)
                return this.keys[0].internalValue;
            CurveKey key1 = this.keys[0];
            CurveKey key2 = this.keys[this.keys.Count - 1];
            float t = position;
            float num1 = 0.0f;
            if ((double)t < (double)key1.position)
            {
                if (this.preLoop == CurveLoopType.Constant)
                    return key1.internalValue;
                if (this.preLoop == CurveLoopType.Linear)
                    return key1.internalValue - key1.tangentIn * (key1.position - t);
                if (!this.keys.IsCacheAvailable)
                    this.keys.ComputeCacheValues();
                float num2 = this.CalcCycle(t);
                float num3 = t - (key1.position + num2 * this.keys.TimeRange);
                if (this.preLoop == CurveLoopType.Cycle)
                    t = key1.position + num3;
                else if (this.preLoop == CurveLoopType.CycleOffset)
                {
                    t = key1.position + num3;
                    num1 = (key2.internalValue - key1.internalValue) * num2;
                }
                else
                    t = ((int)num2 & 1) != 0 ? key2.position - num3 : key1.position + num3;
            }
            else if ((double)key2.position < (double)t)
            {
                if (this.postLoop == CurveLoopType.Constant)
                    return key2.internalValue;
                if (this.postLoop == CurveLoopType.Linear)
                    return key2.internalValue - key2.tangentOut * (key2.position - t);
                if (!this.keys.IsCacheAvailable)
                    this.keys.ComputeCacheValues();
                float num2 = this.CalcCycle(t);
                float num3 = t - (key1.position + num2 * this.keys.TimeRange);
                if (this.postLoop == CurveLoopType.Cycle)
                    t = key1.position + num3;
                else if (this.postLoop == CurveLoopType.CycleOffset)
                {
                    t = key1.position + num3;
                    num1 = (key2.internalValue - key1.internalValue) * num2;
                }
                else
                    t = ((int)num2 & 1) != 0 ? key2.position - num3 : key1.position + num3;
            }
            CurveKey k0 = (CurveKey)null;
            CurveKey k1 = (CurveKey)null;
            float segment = this.FindSegment(t, ref k0, ref k1);
            return num1 + Curve.Hermite(k0, k1, segment);
        }

        private float CalcCycle(float t)
        {
            float num = (t - this.keys[0].position) * this.keys.InvTimeRange;
            if ((double)num < 0.0)
                --num;
            return (float)(int)num;
        }

        private float FindSegment(float t, ref CurveKey k0, ref CurveKey k1)
        {
            float num1 = t;
            k0 = this.keys[0];
            for (int index = 1; index < this.keys.Count; ++index)
            {
                k1 = this.keys[index];
                if ((double)k1.position >= (double)t)
                {
                    double position1 = (double)k0.position;
                    double position2 = (double)k1.position;
                    double num2 = (double)t;
                    double num3 = position2 - position1;
                    num1 = 0.0f;
                    if (num3 > 1E-10)
                    {
                        num1 = (float)((num2 - position1) / num3);
                        break;
                    }
                    break;
                }
                k0 = k1;
            }
            return num1;
        }

        private static float Hermite(CurveKey k0, CurveKey k1, float t)
        {
            if (k0.Continuity == CurveContinuity.Step)
            {
                if ((double)t >= 1.0)
                    return k1.internalValue;
                return k0.internalValue;
            }
            float num1 = t * t;
            float num2 = num1 * t;
            float internalValue1 = k0.internalValue;
            float internalValue2 = k1.internalValue;
            float tangentOut = k0.tangentOut;
            float tangentIn = k1.tangentIn;
            return (float)((double)internalValue1 * (2.0 * (double)num2 - 3.0 * (double)num1 + 1.0) + (double)internalValue2 * (-2.0 * (double)num2 + 3.0 * (double)num1) + (double)tangentOut * ((double)num2 - 2.0 * (double)num1 + (double)t) + (double)tangentIn * ((double)num2 - (double)num1));
        }
    }

    public enum CurveContinuity
    {
        Smooth,
        Step,
    }
    [Serializable]
    public class CurveKey : IEquatable<CurveKey>, IComparable<CurveKey>
    {
        internal float position;
        internal float internalValue;
        internal float tangentOut;
        internal float tangentIn;
        internal CurveContinuity continuity;

        public float Position
        {
            get
            {
                return this.position;
            }
        }

        public float Value
        {
            get
            {
                return this.internalValue;
            }
            set
            {
                this.internalValue = value;
            }
        }

        public float TangentIn
        {
            get
            {
                return this.tangentIn;
            }
            set
            {
                this.tangentIn = value;
            }
        }

        public float TangentOut
        {
            get
            {
                return this.tangentOut;
            }
            set
            {
                this.tangentOut = value;
            }
        }

        public CurveContinuity Continuity
        {
            get
            {
                return this.continuity;
            }
            set
            {
                this.continuity = value;
            }
        }

        public CurveKey(float position, float value)
        {
            this.position = position;
            this.internalValue = value;
        }

        public CurveKey(float position, float value, float tangentIn, float tangentOut)
        {
            this.position = position;
            this.internalValue = value;
            this.tangentIn = tangentIn;
            this.tangentOut = tangentOut;
        }

        public CurveKey(
          float position,
          float value,
          float tangentIn,
          float tangentOut,
          CurveContinuity continuity)
        {
            this.position = position;
            this.internalValue = value;
            this.tangentIn = tangentIn;
            this.tangentOut = tangentOut;
            this.continuity = continuity;
        }

        public CurveKey Clone()
        {
            return new CurveKey(this.position, this.internalValue, this.tangentIn, this.tangentOut, this.continuity);
        }

        public bool Equals(CurveKey other)
        {
            if (other != (CurveKey)null && (double)other.position == (double)this.position && ((double)other.internalValue == (double)this.internalValue && (double)other.tangentIn == (double)this.tangentIn) && (double)other.tangentOut == (double)this.tangentOut)
                return other.continuity == this.continuity;
            return false;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as CurveKey);
        }

        public override int GetHashCode()
        {
            return this.position.GetHashCode() + this.internalValue.GetHashCode() + this.tangentIn.GetHashCode() + this.tangentOut.GetHashCode() + this.continuity.GetHashCode();
        }

        public static bool operator ==(CurveKey a, CurveKey b)
        {
            bool flag1 = null == (object)a;
            bool flag2 = null == (object)b;
            return flag1 || flag2 ? flag1 == flag2 : a.Equals(b);
        }

        public static bool operator !=(CurveKey a, CurveKey b)
        {
            bool flag1 = a == (CurveKey)null;
            bool flag2 = b == (CurveKey)null;
            return flag1 || flag2 ? flag1 != flag2 : (double)a.position != (double)b.position || (double)a.internalValue != (double)b.internalValue || ((double)a.tangentIn != (double)b.tangentIn || (double)a.tangentOut != (double)b.tangentOut) || a.continuity != b.continuity;
        }

        public int CompareTo(CurveKey other)
        {
            if ((double)this.position == (double)other.position)
                return 0;
            return (double)this.position >= (double)other.position ? 1 : -1;
        }
    }
    [Serializable]
    public class CurveKeyCollection : ICollection<CurveKey>, IEnumerable<CurveKey>, IEnumerable
    {
        private List<CurveKey> Keys = new List<CurveKey>();
        internal bool IsCacheAvailable = true;
        internal float TimeRange;
        internal float InvTimeRange;

        public int IndexOf(CurveKey item)
        {
            return this.Keys.IndexOf(item);
        }

        public void RemoveAt(int index)
        {
            this.Keys.RemoveAt(index);
            this.IsCacheAvailable = false;
        }

        public CurveKey this[int index]
        {
            get
            {
                return this.Keys[index];
            }
            set
            {
                if (value == (CurveKey)null)
                    throw new ArgumentNullException();
                if ((double)this.Keys[index].Position == (double)value.Position)
                {
                    this.Keys[index] = value;
                }
                else
                {
                    this.Keys.RemoveAt(index);
                    this.Add(value);
                }
            }
        }

        public void Add(CurveKey item)
        {
            if (item == (CurveKey)null)
                throw new ArgumentNullException();
            int index = this.Keys.BinarySearch(item);
            if (index >= 0)
            {
                while (index < this.Keys.Count && (double)item.Position == (double)this.Keys[index].Position)
                    ++index;
            }
            else
                index = ~index;
            this.Keys.Insert(index, item);
            this.IsCacheAvailable = false;
        }

        public void Clear()
        {
            this.Keys.Clear();
            this.TimeRange = this.InvTimeRange = 0.0f;
            this.IsCacheAvailable = false;
        }

        public bool Contains(CurveKey item)
        {
            return this.Keys.Contains(item);
        }

        public void CopyTo(CurveKey[] array, int arrayIndex)
        {
            this.Keys.CopyTo(array, arrayIndex);
            this.IsCacheAvailable = false;
        }

        public int Count
        {
            get
            {
                return this.Keys.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool Remove(CurveKey item)
        {
            this.IsCacheAvailable = false;
            return this.Keys.Remove(item);
        }

        public IEnumerator<CurveKey> GetEnumerator()
        {
            return (IEnumerator<CurveKey>)this.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.Keys).GetEnumerator();
        }

        public CurveKeyCollection Clone()
        {
            return new CurveKeyCollection()
            {
                Keys = new List<CurveKey>((IEnumerable<CurveKey>)this.Keys),
                InvTimeRange = this.InvTimeRange,
                TimeRange = this.TimeRange,
                IsCacheAvailable = true
            };
        }

        internal void ComputeCacheValues()
        {
            this.TimeRange = this.InvTimeRange = 0.0f;
            if (this.Keys.Count > 1)
            {
                this.TimeRange = this.Keys[this.Keys.Count - 1].Position - this.Keys[0].Position;
                if ((double)this.TimeRange > 1.40129846432482E-45)
                    this.InvTimeRange = 1f / this.TimeRange;
            }
            this.IsCacheAvailable = true;
        }
    }
    public enum CurveLoopType
    {
        Constant,
        Cycle,
        CycleOffset,
        Oscillate,
        Linear,
    }
    public enum CurveTangent
    {
        Flat,
        Linear,
        Smooth,
    }
}
