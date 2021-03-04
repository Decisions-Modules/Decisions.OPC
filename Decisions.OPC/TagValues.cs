using DecisionsFramework.Design.Flow.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.OPC
{
    [DataContract]
    public class BaseTagValueWrapper
    {
        [DataMember]
        public BaseTagValue[] Values { get; set; }
    }

    [DataContract]
    [KnownType(typeof(SByteTagValue))]
    [KnownType(typeof(ByteTagValue))]
    [KnownType(typeof(ShortTagValue))]
    [KnownType(typeof(UShortTagValue))]
    [KnownType(typeof(IntTagValue))]
    [KnownType(typeof(UIntTagValue))]
    [KnownType(typeof(LongTagValue))]
    [KnownType(typeof(ULongTagValue))]
    [KnownType(typeof(FloatTagValue))]
    [KnownType(typeof(DoubleTagValue))]
    [KnownType(typeof(DecimalTagValue))]
    [KnownType(typeof(BoolTagValue))]
    [KnownType(typeof(DateTimeTagValue))]
    [KnownType(typeof(StringTagValue))]
    [KnownType(typeof(SByteArrayTagValue))]
    [KnownType(typeof(ByteArrayTagValue))]
    [KnownType(typeof(ShortArrayTagValue))]
    [KnownType(typeof(UShortArrayTagValue))]
    [KnownType(typeof(IntArrayTagValue))]
    [KnownType(typeof(UIntArrayTagValue))]
    [KnownType(typeof(LongArrayTagValue))]
    [KnownType(typeof(ULongArrayTagValue))]
    [KnownType(typeof(FloatArrayTagValue))]
    [KnownType(typeof(DoubleArrayTagValue))]
    [KnownType(typeof(DecimalArrayTagValue))]
    [KnownType(typeof(BoolArrayTagValue))]
    [KnownType(typeof(DateTimeArrayTagValue))]
    [KnownType(typeof(StringArrayTagValue))]
    public abstract class BaseTagValue
    {
        [DataMember]
        public string Path { get; set; }
        [DataMember]
        public OpcQuality Quality { get; set; }
        [DataMember]
        public DateTime TimeStamp { get; set; }
    }

    [DataContract]
    public class SByteTagValue : BaseTagValue
    {
        [DataMember]
        public sbyte Value { get; set; }
    }
    [DataContract]
    public class ByteTagValue : BaseTagValue
    {
        [DataMember]
        public byte Value { get; set; }
    }
    [DataContract]
    public class ShortTagValue : BaseTagValue
    {
        [DataMember]
        public short Value { get; set; }
    }
    [DataContract]
    public class UShortTagValue : BaseTagValue
    {
        [DataMember]
        public ushort Value { get; set; }
    }
    [DataContract]
    public class IntTagValue : BaseTagValue
    {
        [DataMember]
        public int Value { get; set; }
    }
    [DataContract]
    public class UIntTagValue : BaseTagValue
    {
        [DataMember]
        public uint Value { get; set; }
    }
    [DataContract]
    public class LongTagValue : BaseTagValue
    {
        [DataMember]
        public long Value { get; set; }
    }
    [DataContract]
    public class ULongTagValue : BaseTagValue
    {
        [DataMember]
        public ulong Value { get; set; }
    }
    [DataContract]
    public class FloatTagValue : BaseTagValue
    {
        [DataMember]
        public float Value { get; set; }
    }
    [DataContract]
    public class DoubleTagValue : BaseTagValue
    {
        [DataMember]
        public double Value { get; set; }
    }
    [DataContract]
    public class DecimalTagValue : BaseTagValue
    {
        [DataMember]
        public decimal Value { get; set; }
    }
    [DataContract]
    public class BoolTagValue : BaseTagValue
    {
        [DataMember]
        public bool Value { get; set; }
    }
    [DataContract]
    public class DateTimeTagValue : BaseTagValue
    {
        [DataMember]
        public DateTime Value { get; set; }
    }
    [DataContract]
    public class StringTagValue : BaseTagValue
    {
        [DataMember]
        public string Value { get; set; }
    }

    //arrays
    [DataContract]
    public class SByteArrayTagValue : BaseTagValue
    {
        [DataMember]
        public sbyte[] Value { get; set; }
    }
    [DataContract]
    public class ByteArrayTagValue : BaseTagValue
    {
        [DataMember]
        public byte[] Value { get; set; }
    }
    [DataContract]
    public class ShortArrayTagValue : BaseTagValue
    {
        [DataMember]
        public short[] Value { get; set; }
    }
    [DataContract]
    public class UShortArrayTagValue : BaseTagValue
    {
        [DataMember]
        public ushort[] Value { get; set; }
    }
    [DataContract]
    public class IntArrayTagValue : BaseTagValue
    {
        [DataMember]
        public int[] Value { get; set; }
    }
    [DataContract]
    public class UIntArrayTagValue : BaseTagValue
    {
        [DataMember]
        public uint[] Value { get; set; }
    }
    [DataContract]
    public class LongArrayTagValue : BaseTagValue
    {
        [DataMember]
        public long[] Value { get; set; }
    }
    [DataContract]
    public class ULongArrayTagValue : BaseTagValue
    {
        [DataMember]
        public ulong[] Value { get; set; }
    }
    [DataContract]
    public class FloatArrayTagValue : BaseTagValue
    {
        [DataMember]
        public float[] Value { get; set; }
    }
    [DataContract]
    public class DoubleArrayTagValue : BaseTagValue
    {
        [DataMember]
        public double[] Value { get; set; }
    }
    [DataContract]
    public class DecimalArrayTagValue : BaseTagValue
    {
        [DataMember]
        public decimal[] Value { get; set; }
    }
    [DataContract]
    public class BoolArrayTagValue : BaseTagValue
    {
        [DataMember]
        public bool[] Value { get; set; }
    }
    [DataContract]
    public class DateTimeArrayTagValue : BaseTagValue
    {
        [DataMember]
        public DateTime[] Value { get; set; }
    }
    [DataContract]
    public class StringArrayTagValue : BaseTagValue
    {
        [DataMember]
        public string[] Value { get; set; }
    }

    internal class TagValueUtils
    {
        public static Type GetTagValueType(Type valueType)
        {
            if (valueType == typeof(sbyte)) return typeof(SByteTagValue);
            if (valueType == typeof(byte)) return typeof(ByteTagValue);
            if (valueType == typeof(short)) return typeof(ShortTagValue);
            if (valueType == typeof(ushort)) return typeof(UShortTagValue);
            if (valueType == typeof(int)) return typeof(IntTagValue);
            if (valueType == typeof(uint)) return typeof(UIntTagValue);
            if (valueType == typeof(long)) return typeof(LongTagValue);
            if (valueType == typeof(ulong)) return typeof(ULongTagValue);
            if (valueType == typeof(float)) return typeof(FloatTagValue);
            if (valueType == typeof(double)) return typeof(DoubleTagValue);
            if (valueType == typeof(decimal)) return typeof(DecimalTagValue);
            if (valueType == typeof(bool)) return typeof(BoolTagValue);
            if (valueType == typeof(DateTime)) return typeof(DateTimeTagValue);
            if (valueType == typeof(string)) return typeof(StringTagValue);
            if (valueType == typeof(sbyte[])) return typeof(SByteArrayTagValue);
            if (valueType == typeof(byte[])) return typeof(ByteArrayTagValue);
            if (valueType == typeof(short[])) return typeof(ShortArrayTagValue);
            if (valueType == typeof(ushort[])) return typeof(UShortArrayTagValue);
            if (valueType == typeof(int[])) return typeof(IntArrayTagValue);
            if (valueType == typeof(uint[])) return typeof(UIntArrayTagValue);
            if (valueType == typeof(long[])) return typeof(LongArrayTagValue);
            if (valueType == typeof(ulong[])) return typeof(ULongArrayTagValue);
            if (valueType == typeof(float[])) return typeof(FloatArrayTagValue);
            if (valueType == typeof(double[])) return typeof(DoubleArrayTagValue);
            if (valueType == typeof(decimal[])) return typeof(DecimalArrayTagValue);
            if (valueType == typeof(bool[])) return typeof(BoolArrayTagValue);
            if (valueType == typeof(DateTime[])) return typeof(DateTimeArrayTagValue);
            if (valueType == typeof(string[])) return typeof(StringArrayTagValue);
            return typeof(StringTagValue); // try defaulting to string
        }

        public static DataDescription GetDataDescriptionFromTagType(Type tagType, string dataName)
        {
            if (tagType == typeof(SByteTagValue)) return new DataDescription(typeof(sbyte), dataName, false);
            if (tagType == typeof(ByteTagValue)) return new DataDescription(typeof(byte), dataName, false);
            if (tagType == typeof(ShortTagValue)) return new DataDescription(typeof(short), dataName, false);
            if (tagType == typeof(UShortTagValue)) return new DataDescription(typeof(ushort), dataName, false);
            if (tagType == typeof(IntTagValue)) return new DataDescription(typeof(int), dataName, false);
            if (tagType == typeof(UIntTagValue)) return new DataDescription(typeof(uint), dataName, false);
            if (tagType == typeof(LongTagValue)) return new DataDescription(typeof(long), dataName, false);
            if (tagType == typeof(ULongTagValue)) return new DataDescription(typeof(ulong), dataName, false);
            if (tagType == typeof(FloatTagValue)) return new DataDescription(typeof(float), dataName, false);
            if (tagType == typeof(DoubleTagValue)) return new DataDescription(typeof(double), dataName, false);
            if (tagType == typeof(DecimalTagValue)) return new DataDescription(typeof(decimal), dataName, false);
            if (tagType == typeof(BoolTagValue)) return new DataDescription(typeof(bool), dataName, false);
            if (tagType == typeof(DateTimeTagValue)) return new DataDescription(typeof(DateTime), dataName, false);
            if (tagType == typeof(StringTagValue)) return new DataDescription(typeof(string), dataName, false);
            if (tagType == typeof(SByteArrayTagValue)) return new DataDescription(typeof(sbyte), dataName, true);
            if (tagType == typeof(ByteArrayTagValue)) return new DataDescription(typeof(byte), dataName, true);
            if (tagType == typeof(ShortArrayTagValue)) return new DataDescription(typeof(short), dataName, true);
            if (tagType == typeof(UShortArrayTagValue)) return new DataDescription(typeof(ushort), dataName, true);
            if (tagType == typeof(IntArrayTagValue)) return new DataDescription(typeof(int), dataName, true);
            if (tagType == typeof(UIntArrayTagValue)) return new DataDescription(typeof(uint), dataName, true);
            if (tagType == typeof(LongArrayTagValue)) return new DataDescription(typeof(long), dataName, true);
            if (tagType == typeof(ULongArrayTagValue)) return new DataDescription(typeof(ulong), dataName, true);
            if (tagType == typeof(FloatArrayTagValue)) return new DataDescription(typeof(float), dataName, true);
            if (tagType == typeof(DoubleArrayTagValue)) return new DataDescription(typeof(double), dataName, true);
            if (tagType == typeof(DecimalArrayTagValue)) return new DataDescription(typeof(decimal), dataName, true);
            if (tagType == typeof(BoolArrayTagValue)) return new DataDescription(typeof(bool), dataName, true);
            if (tagType == typeof(DateTimeArrayTagValue)) return new DataDescription(typeof(DateTime), dataName, true);
            if (tagType == typeof(StringArrayTagValue)) return new DataDescription(typeof(string), dataName, true);
            throw new Exception("Unknown tag type");
        }

        public static object GetObjectValueFromTag(BaseTagValue tag)
        {
            if (tag is SByteTagValue) return (tag as SByteTagValue).Value;
            if (tag is ByteTagValue) return (tag as ByteTagValue).Value;
            if (tag is ShortTagValue) return (tag as ShortTagValue).Value;
            if (tag is UShortTagValue) return (tag as UShortTagValue).Value;
            if (tag is IntTagValue) return (tag as IntTagValue).Value;
            if (tag is UIntTagValue) return (tag as UIntTagValue).Value;
            if (tag is LongTagValue) return (tag as LongTagValue).Value;
            if (tag is ULongTagValue) return (tag as ULongTagValue).Value;
            if (tag is FloatTagValue) return (tag as FloatTagValue).Value;
            if (tag is DoubleTagValue) return (tag as DoubleTagValue).Value;
            if (tag is DecimalTagValue) return (tag as DecimalTagValue).Value;
            if (tag is BoolTagValue) return (tag as BoolTagValue).Value;
            if (tag is DateTimeTagValue) return (tag as DateTimeTagValue).Value;
            if (tag is StringTagValue) return (tag as StringTagValue).Value;
            if (tag is SByteArrayTagValue) return (tag as SByteArrayTagValue).Value;
            if (tag is ByteArrayTagValue) return (tag as ByteArrayTagValue).Value;
            if (tag is ShortArrayTagValue) return (tag as ShortArrayTagValue).Value;
            if (tag is UShortArrayTagValue) return (tag as UShortArrayTagValue).Value;
            if (tag is IntArrayTagValue) return (tag as IntArrayTagValue).Value;
            if (tag is UIntArrayTagValue) return (tag as UIntArrayTagValue).Value;
            if (tag is LongArrayTagValue) return (tag as LongArrayTagValue).Value;
            if (tag is ULongArrayTagValue) return (tag as ULongArrayTagValue).Value;
            if (tag is FloatArrayTagValue) return (tag as FloatArrayTagValue).Value;
            if (tag is DoubleArrayTagValue) return (tag as DoubleArrayTagValue).Value;
            if (tag is DecimalArrayTagValue) return (tag as DecimalArrayTagValue).Value;
            if (tag is BoolArrayTagValue) return (tag as BoolArrayTagValue).Value;
            if (tag is DateTimeArrayTagValue) return (tag as DateTimeArrayTagValue).Value;
            if (tag is StringArrayTagValue) return (tag as StringArrayTagValue).Value;
            throw new Exception("Unknown tag value type");
        }

        public static BaseTagValue GetTagWithValue(Type typeOfValue, object value)
        {
            if (typeOfValue == typeof(sbyte)) return new SByteTagValue { Value = (sbyte)value };
            if (typeOfValue == typeof(byte)) return new ByteTagValue { Value = (byte)value };
            if (typeOfValue == typeof(short)) return new ShortTagValue { Value = (short)value };
            if (typeOfValue == typeof(ushort)) return new UShortTagValue { Value = (ushort)value };
            if (typeOfValue == typeof(int)) return new IntTagValue { Value = (int)value };
            if (typeOfValue == typeof(uint)) return new UIntTagValue { Value = (uint)value };
            if (typeOfValue == typeof(long)) return new LongTagValue { Value = (long)value };
            if (typeOfValue == typeof(ulong)) return new ULongTagValue { Value = (ulong)value };
            if (typeOfValue == typeof(float)) return new FloatTagValue { Value = (float)value };
            if (typeOfValue == typeof(double)) return new DoubleTagValue { Value = (double)value };
            if (typeOfValue == typeof(decimal)) return new DecimalTagValue { Value = (decimal)value };
            if (typeOfValue == typeof(bool)) return new BoolTagValue { Value = (bool)value };
            if (typeOfValue == typeof(DateTime)) return new DateTimeTagValue { Value = (DateTime)value };
            if (typeOfValue == typeof(string)) return new StringTagValue { Value = (string)value };
            if (typeOfValue == typeof(sbyte[])) return new SByteArrayTagValue { Value = (sbyte[])value };
            if (typeOfValue == typeof(byte[])) return new ByteArrayTagValue { Value = (byte[])value };
            if (typeOfValue == typeof(short[])) return new ShortArrayTagValue { Value = (short[])value };
            if (typeOfValue == typeof(ushort[])) return new UShortArrayTagValue { Value = (ushort[])value };
            if (typeOfValue == typeof(int[])) return new IntArrayTagValue { Value = (int[])value };
            if (typeOfValue == typeof(uint[])) return new UIntArrayTagValue { Value = (uint[])value };
            if (typeOfValue == typeof(long[])) return new LongArrayTagValue { Value = (long[])value };
            if (typeOfValue == typeof(ulong[])) return new ULongArrayTagValue { Value = (ulong[])value };
            if (typeOfValue == typeof(float[])) return new FloatArrayTagValue { Value = (float[])value };
            if (typeOfValue == typeof(double[])) return new DoubleArrayTagValue { Value = (double[])value };
            if (typeOfValue == typeof(decimal[])) return new DecimalArrayTagValue { Value = (decimal[])value };
            if (typeOfValue == typeof(bool[])) return new BoolArrayTagValue { Value = (bool[])value };
            if (typeOfValue == typeof(DateTime[])) return new DateTimeArrayTagValue { Value = (DateTime[])value };
            if (typeOfValue == typeof(string[])) return new StringArrayTagValue { Value = (string[])value };
            return new StringTagValue { Value = value.ToString() }; // try defaulting to string
        }
    }
}
