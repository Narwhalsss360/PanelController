using System.Text;
using System.Text.RegularExpressions;

namespace PanelController.PanelObjects
{
    public interface IPanelSettable : IPanelObject
    {
        public class SettableValue : IFormattable
        {
            private delegate object? ConvertBytesToObject(byte[] value, int startIndex);

            private delegate byte[]? ConvertObjectToBytes(object @object);

            private static readonly Type[] s_types =
            {
                typeof(Int16), //Should be signed byte, but doesnt exist.
                typeof(Int16),
                typeof(Int32),
                typeof(Int64),
                typeof(float),
                typeof(double),
                typeof(decimal),
                typeof(string),
                typeof(byte[]),
                typeof(byte),
                typeof(UInt16),
                typeof(UInt32),
                typeof(UInt64),
            };

            private static readonly ConvertBytesToObject[] s_convertersToObject =
            {
                (byte[] value, int startIndex) =>
                {
                    return BitConverter.ToInt16(new byte[] { value[startIndex], 0 });
                },
                (value, startIndex) => BitConverter.ToInt16(value, startIndex),
                (value, startIndex) => BitConverter.ToInt32(value, startIndex),
                (value, startIndex) => BitConverter.ToInt64(value, startIndex),
                (value, startIndex) => BitConverter.ToHalf(value, startIndex),
                (value, startIndex) => BitConverter.ToDouble(value, startIndex),
                (value, startIndex) =>
                {
                    if (decimal.TryParse(Encoding.UTF8.GetString(value.Skip(startIndex).ToArray()), out decimal dec))
                        return dec;
                    return null;
                },
                (value, startIndex) => Encoding.UTF8.GetString(value.Skip(startIndex).ToArray()),
                (value, startIndex) => value.Skip(startIndex).ToArray(),
                (value, startIndex) => value[startIndex],
                (value, startIndex) => BitConverter.ToUInt16(value, startIndex),
                (value, startIndex) => BitConverter.ToUInt32(value, startIndex),
                (value, startIndex) => BitConverter.ToUInt64(value, startIndex)
            };

            private static readonly Dictionary<Type, ConvertObjectToBytes> s_typeToObjectToBytes = new()
            {
                { typeof(byte), (obj) => new byte[] { (byte)obj } },
                { typeof(Int16), (obj) => BitConverter.GetBytes((Int16)obj) },
                { typeof(Int32), (obj) => BitConverter.GetBytes((Int32)obj) },
                { typeof(Int64), (obj) => BitConverter.GetBytes((Int64)obj) },
                { typeof(float), (obj) => BitConverter.GetBytes((float)obj) },
                { typeof(double), (obj) => BitConverter.GetBytes((double)obj) },
                { typeof(decimal), (obj) => Encoding.UTF8.GetBytes(((decimal)obj).ToString()) },
                { typeof(string), (obj) => Encoding.UTF8.GetBytes((string)obj) },
                { typeof(byte[]), (obj) => (byte[])obj },
                { typeof(UInt16), (obj) => BitConverter.GetBytes((UInt16)obj) },
                { typeof(UInt32), (obj) => BitConverter.GetBytes((UInt32)obj) },
                { typeof(UInt64), (obj) => BitConverter.GetBytes((UInt64)obj) }
            };

            private static readonly Dictionary<Type, int> s_typesToSizes = new()
            {
                { typeof(byte), sizeof(byte) },
                { typeof(Int16), sizeof(Int16) },
                { typeof(Int32), sizeof(Int32) },
                { typeof(Int64), sizeof(Int64) },
                { typeof(float), sizeof(float) },
                { typeof(double), sizeof(double) },
                { typeof(decimal), 0 },
                { typeof(string), 0 },
                { typeof(byte[]), 0 },
                { typeof(UInt16), sizeof(UInt16) },
                { typeof(UInt32), sizeof(UInt32) },
                { typeof(UInt64), sizeof(UInt64) }
            };

            private static readonly Dictionary<int, int> s_indexedTypesToSizes = new()
            {
                { (int)IndexedTypes.I_8, sizeof(byte) },
                { (int)IndexedTypes.I_16, sizeof(Int16) },
                { (int)IndexedTypes.I_32, sizeof(Int32) },
                { (int)IndexedTypes.I_64, sizeof(Int64) },
                { (int)IndexedTypes.I_float, sizeof(float) },
                { (int)IndexedTypes.I_double, sizeof(double) },
                { (int)IndexedTypes.S_decimal, 0 },
                { (int)IndexedTypes.S_string, 0 },
                { (int)IndexedTypes.D_bytes, 0 },
            };

            private enum IndexedTypes
            {
                I_8,
                I_16,
                I_32,
                I_64,
                I_float,
                I_double,
                S_decimal,
                S_string,
                D_bytes
            }

            private enum DataByteIndices
            {
                Meta,
                Data
            }

            private enum MetaBits
            {
                Signed = 7
            }

            public readonly byte[] Data;

            public bool IsSigned
            {
                get => (Data[(int)DataByteIndices.Meta] & (1 << (int)MetaBits.Signed)) > 0;
            }

            private int IndexedType
            {
                get => (Data[(int)DataByteIndices.Meta]) & ~(1 << (int)MetaBits.Signed);
            }

            private bool IsSimpleInt
            {
                get => IndexedType <= (int)IndexedTypes.I_64;
            }

            public Type DataType
            {
                get => IsSimpleInt ? (IsSigned ? s_types[IndexedType + (int)IndexedTypes.D_bytes] : s_types[IndexedType]) : (s_types[IndexedType]);
            }

            private ConvertBytesToObject Converter
            {
                get => IsSimpleInt ? (IsSigned ? s_convertersToObject[IndexedType + (int)IndexedTypes.D_bytes] : s_convertersToObject[IndexedType]) : (s_convertersToObject[IndexedType]);
            }

            public object? Value
            {
                get => Converter(Data, 1);
            }

            public SettableValue(byte[] data)
            {
                if (!IsValidData(data))
                    throw new InvalidOperationException("data was invalid");
                Data = data;
            }

            public static byte[] CreateData(object data)
            {
                int idx = Array.IndexOf(s_types, data.GetType());
                if (idx == -1)
                    return Array.Empty<byte>();

                if (idx == 0)
                    idx = 1;

                List<byte> bytes = new() { 0 };

                if (idx <= (int)IndexedTypes.I_64)
                    bytes[0] = (byte)(((int)IndexedTypes.D_bytes - idx) | (1 << (int)MetaBits.Signed));
                else
                    bytes[0] = (byte)idx;

                if (s_typeToObjectToBytes[data.GetType()](data) is byte[] dataBytes)
                    bytes.AddRange(dataBytes);
                else
                    return Array.Empty<byte>();

                return bytes.ToArray();
            }

            public static bool IsValidData(byte[] data)
            {
                if (data.Length < 2)
                    return false;
                if (data[0] > (byte)IndexedTypes.D_bytes)
                    return false;
                if (s_indexedTypesToSizes[data[0]] != 0)
                    if (s_indexedTypesToSizes[data[0]] + 1 != data.Length)
                        return false;

                return true;
            }

            public string ToString(string? format, IFormatProvider? formatProvider) => Value?.ToString() ?? "";
        }

        public object? Set(SettableValue? value);
    }
}
