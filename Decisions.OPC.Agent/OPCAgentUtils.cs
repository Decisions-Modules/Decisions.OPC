using DecisionsFramework.ServiceLayer.Services.ContextData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.OPC.Agent
{
    internal static class OPCAgentUtils
    {
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
            if (typeOfValue == typeof(sbyte[])) return new SByteArrayTagValue { Value = (sbyte[])ChangeType(value, typeof(sbyte)) };
            if (typeOfValue == typeof(byte[])) return new ByteArrayTagValue { Value = (byte[])ChangeType(value, typeof(byte)) };
            if (typeOfValue == typeof(short[])) return new ShortArrayTagValue { Value = (short[])ChangeType(value, typeof(short)) };
            if (typeOfValue == typeof(ushort[])) return new UShortArrayTagValue { Value = (ushort[])ChangeType(value, typeof(ushort)) };
            if (typeOfValue == typeof(int[])) return new IntArrayTagValue { Value = (int[])ChangeType(value, typeof(int)) };
            if (typeOfValue == typeof(uint[])) return new UIntArrayTagValue { Value = (uint[])ChangeType(value, typeof(uint)) };
            if (typeOfValue == typeof(long[])) return new LongArrayTagValue { Value = (long[])ChangeType(value, typeof(long)) };
            if (typeOfValue == typeof(ulong[])) return new ULongArrayTagValue { Value = (ulong[])ChangeType(value, typeof(ulong)) };
            if (typeOfValue == typeof(float[])) return new FloatArrayTagValue { Value = (float[])ChangeType(value, typeof(float)) };
            if (typeOfValue == typeof(double[])) return new DoubleArrayTagValue { Value = (double[])ChangeType(value, typeof(double)) };
            if (typeOfValue == typeof(decimal[])) return new DecimalArrayTagValue { Value = (decimal[])ChangeType(value, typeof(decimal)) };
            if (typeOfValue == typeof(bool[])) return new BoolArrayTagValue { Value = (bool[])ChangeType(value, typeof(bool)) };
            if (typeOfValue == typeof(DateTime[])) return new DateTimeArrayTagValue { Value = (DateTime[])ChangeType(value, typeof(DateTime)) };
            if (typeOfValue == typeof(string[])) return new StringArrayTagValue { Value = (string[])ChangeType(value, typeof(string)) };
            return new StringTagValue { Value = value.ToString() }; // try defaulting to string
        }

        //copied from typeutility, nullable stuff removed:
        internal static Type GetInnerType(this Type type)
        {
            return type.IsArray ? type.GetElementType() : type;
        }

        internal static object ChangeType(object value, Type conversionType)
        {
            if (value == null)
                return null;

            if (value.GetType() == conversionType || conversionType.IsAssignableFrom(value.GetType()))
                return value;

            if (value.GetType().IsArray && typeof(IConvertible).IsAssignableFrom(conversionType.GetInnerType()))
            {
                ArrayList result = new ArrayList();

                foreach (var v in (value as Array))
                {
                    result.Add(Convert.ChangeType(v, conversionType.GetInnerType()));
                }

                return result.ToArray(conversionType.GetInnerType());
            }
            else if (value.GetType().IsArray && value.GetType().GetInnerType() != conversionType.GetInnerType())
            {
                ArrayList result = new ArrayList();

                foreach (var v in (value as Array))
                {
                    result.Add(ChangeType(v, conversionType.GetInnerType()));
                }

                return result.ToArray(conversionType.GetInnerType());
            }
            else if (conversionType.IsPrimitive || value is IConvertible)
            {
                return Convert.ChangeType(value, conversionType);
            }

            throw new Exception(string.Format("Cannot convert from {0} to {1}", value.GetType().Name, conversionType.Name));
        }

    }

    public static class DataPairExtensions
    {
        // from Decisions.Agent.Handlers.Helpers.DataPairExtensions
        public static T GetValueByKey<T>(this DataPair[] data, string key)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");

            DataPair pair = data.FirstOrDefault(d => (d.Name == key));
            if (pair == null)
                throw new Exception(string.Format("Data is not found by name: {0}", key));
            if (pair.OutputValue == null)
                return default(T);
            if (false == pair.OutputValue is T)
                throw new Exception(string.Format("Value ({0}) is not type of {1}", key, typeof(T).Name));
            return (T)pair.OutputValue;
        }

    }
}
