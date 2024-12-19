using System.Text;

namespace PanelController.PanelObjects
{
    public interface IPanelSettable : IPanelObject
    {
        public object? Set(SettableValue? value);

        public class SettableValue
        {
            public delegate byte[] StringToBytesFunction(string s);

            public enum Types
            {
                Bytes,
                Integer,
                Float,
                String
            }

            public object? Value { get; private set; } = null;

            public SettableValue(byte[] settableData)
            {
                if (IsValidSettableData(settableData))
                    Parse(settableData);
            }

            public static byte[] CreateBytes(object value, StringToBytesFunction? stringToBytes = null, int charSize = 1)
            {
                if (!IsSupportedType(value.GetType()))
                    return Array.Empty<byte>();
                byte[] parsed = CreateBytesSingle(value, stringToBytes);
                byte[] data = new byte[parsed.Length + 1];
                data[0] = CreateMetaByte(value.GetType(), false, charSize);
                Array.Copy(parsed, 0, data, 1, parsed.Length);
                return data;
            }

            public static byte[] CreateBytes<T>(T[] values, StringToBytesFunction? stringToBytes = null, int charSize = 1)
            {
                if (!IsSupportedType(typeof(T)))
                    return Array.Empty<byte>();
                List<byte> bytes = new() { CreateMetaByte(typeof(T), true, charSize) };
                foreach (T value in values)
                    bytes.AddRange(CreateBytesSingle(value, stringToBytes));
                return bytes.ToArray();
            }

            private static byte[] CreateBytesSingle<T>(T value, StringToBytesFunction? strToBytes)
            {
                if (value is null)
                    return Array.Empty<byte>();
                if (strToBytes is not null && value is string str)
                    return strToBytes(str);
                return s_parsers[typeof(T)](value);
            }

            public static byte CreateMetaByte(Type type, bool isArray, int stringSize)
            {
                byte metabyte = 0;

                if (type == typeof(float))
                {
                    metabyte = (byte)Types.Float;
                    metabyte |= sizeof(float) << 4;
                }
                else if (type == typeof(double))
                {
                    metabyte = (byte)Types.Float;
                    metabyte |= sizeof(double) << 4;
                }
                else if (s_integerTypesToSizes.ContainsKey(type))
                {
                    metabyte = (byte)Types.Integer;
                    metabyte |= (byte)(s_integerTypesToSizes[type] << 4);
                }
                else if (type == typeof(string))
                {
                    metabyte = (byte)Types.String;
                    metabyte |= (byte)(stringSize << 4);
                }

                if (isArray)
                    metabyte |= 0b100;

                if (s_signedTypes.Contains(type))
                    metabyte |= 0b1000;

                return metabyte;
            }

            public static bool IsValidSettableData(byte[] settableData)
            {
                if (settableData.Length == 0)
                    return false;

                byte metabyte = settableData[0];
                Types type = ExtractType(metabyte);
                bool isArray = IsArray(metabyte);
                int size = SizeOf(ExtractIntegerSize(metabyte));
                int dataLength = settableData.Length - 1;

                if (type == Types.Bytes && isArray)
                    return false;

                if (type == Types.Bytes || type == Types.String)
                    return true;

                if (isArray && dataLength % size != 0)
                    return false;

                if (type == Types.Float)
                    return size == 4 || size == 8;

                return true;
            }

            private void Parse(byte[] settableData)
            {
                byte metabyte = settableData[0];
                Types type = ExtractType(metabyte);
                bool isArray = IsArray(metabyte);
                bool signed = Signed(metabyte);
                int size = SizeOf(ExtractIntegerSize(metabyte));
                int dataLength = settableData.Length - 1;
                byte[] data = settableData.Take(1).ToArray();

                if (!isArray)
                {
                    Value = ParseSingle(type, signed, size, data, 0, dataLength);
                    return;
                }

                if (type == Types.String)
                {
                    List<string> strings = new() { "" };
                    for (int i = 0; i < data.Length; i += size)
                    {
                        if (i == 0)
                            continue;
                        strings[^1] += (char)i;
                    }

                    if (strings.Count == 0 && strings[0] == "")
                        Value = Array.Empty<string>();
                    else
                        Value = strings.ToArray();
                    return;
                }

                object[] values = new object[dataLength / size];
                for (int iData = 0, i = 0; iData < dataLength; iData += size, i++)
                    values[i] = ParseSingle(type, signed, size, data, iData, size);
                Value = values;
            }

            private static object ParseSingle(Types type, bool signed, int size, byte[] data, int offset, int count)
            {
                switch (type)
                {
                    case Types.Bytes:
                        return data;
                    case Types.Integer:
                        return ParseSingleInteger(signed, size, data, offset);
                    case Types.Float:
                        if (size == 4)
                            return BitConverter.ToSingle(data, offset);
                        return BitConverter.ToDouble(data, offset);
                    case Types.String:
                        return Encoding.UTF8.GetString(data, offset, count);
                    default:
                        return data;
                }
            }

            private static object ParseSingleInteger(bool signed, int size, byte[] data, int offset)
            {
                if (signed)
                {
                    return size switch
                    {
                        2 => BitConverter.ToInt16(data, offset),
                        4 => BitConverter.ToInt32(data, offset),
                        8 => BitConverter.ToInt64(data, offset),
                        // Captures case 1:
                        _ => (object)BitConverter.ToInt16(new byte[] { data[offset], 0 }),
                    };
                }

                return size switch
                {
                    2 => BitConverter.ToUInt16(data, offset),
                    4 => BitConverter.ToUInt32(data, offset),
                    8 => BitConverter.ToUInt64(data, offset),
                    // Captures case 1:
                    _ => (object)data[offset],
                };
            }

            /* Unused, just for reference
            private enum MetaByteBits
            {
                TypeBitLow,
                TypeBitHigh,
                ArrayFlag,
                SignedFlag,
                SizeLow,
                SizeHigh
            }
            */

            private enum IntegerSizeMetaData
            {
                Size1,
                Size2,
                Size4,
                Size8
            }

            private static readonly Type[] s_supportedTypes = new Type[]
            {
                typeof(byte),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(float),
                typeof(double),
                typeof(string),
                typeof(byte[]),
            };

            private static readonly Type[] s_signedTypes = new Type[]
            {
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(float),
                typeof(double)
            };

            private static Types ExtractType(byte metabyte) => (Types)(metabyte & 0b11);

            private static bool IsArray(byte metabyte) => (metabyte & 0b100) > 0;

            private static bool Signed(byte metabyte) => (metabyte & 0b1000) > 0;

            private static IntegerSizeMetaData ExtractIntegerSize(byte metabyte) => (IntegerSizeMetaData)((metabyte & 0b110000) >> 4);

            private static int SizeOf(IntegerSizeMetaData integerSize) => (int)Math.Pow(2, (int)integerSize);
        
            private static bool IsSupportedType(Type type) => s_supportedTypes.Contains(type);

            private static readonly Dictionary<Type, byte> s_integerTypesToSizes = new()
            {
                { typeof(byte), sizeof(byte) },
                { typeof(short), sizeof(short) },
                { typeof(int), sizeof(int) },
                { typeof(long), sizeof(long) },
                { typeof(ushort), sizeof(ushort) },
                { typeof(uint), sizeof(uint) },
                { typeof(ulong), sizeof(ulong) },
                { typeof(float), sizeof(float) },
                { typeof(double), sizeof(double) }
            };

            private static readonly Dictionary<Type, Func<object, byte[]>> s_parsers = new()
            {
                {
                    typeof(byte),
                    obj => obj is byte b ? new byte[] { b } : Array.Empty<byte>()
                },
                {
                    typeof(short),
                    obj => obj is short s ? BitConverter.GetBytes(s) : Array.Empty<byte>()
                },
                {
                    typeof(int),
                    obj => obj is int i ? BitConverter.GetBytes(i) : Array.Empty<byte>()
                },
                {
                    typeof(long),
                    obj => obj is long l ? BitConverter.GetBytes(l) : Array.Empty<byte>()
                },
                {
                    typeof(ushort),
                    obj => obj is ushort s ? BitConverter.GetBytes(s) : Array.Empty<byte>()                },
                {
                    typeof(uint),
                    obj => obj is uint i ? BitConverter.GetBytes(i) : Array.Empty<byte>()
                },
                {
                    typeof(ulong),
                    obj => obj is ulong l ? BitConverter.GetBytes(l) : Array.Empty<byte>()
                },
                {
                    typeof(float),
                    obj => obj is float f ? BitConverter.GetBytes(BitConverter.SingleToUInt32Bits(f)) : Array.Empty<byte>()
                },
                {
                    typeof(double),
                    obj => obj is double d ? BitConverter.GetBytes(BitConverter.DoubleToUInt64Bits(d)) : Array.Empty<byte>()
                },
                {
                    typeof(string),
                    obj => obj is string str ? Encoding.UTF8.GetBytes(str) : Array.Empty<byte>()
                },
                {
                    typeof(byte[]),
                    obj => obj is byte[] bytes ? bytes : Array.Empty<byte>()
                },
            };
        }
    }
}
