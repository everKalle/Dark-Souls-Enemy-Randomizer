using System.Buffers.Binary;
using System.Globalization;

namespace EnemyRandomizer.Formats.Binary;

/// <summary>
/// Minimal port of Python's <c>struct</c> module for the codes the game formats use: <c>b B ? h H i I f</c>.
/// <list type="bullet">
/// <item><c>@</c> (or no prefix) = native: little-endian, every item aligned to its own size, no trailing padding
/// (EMEVD instruction arguments, MSB rows).</item>
/// <item><c>&lt;</c> = little-endian, packed; <c>&gt;</c> = big-endian, packed.</item>
/// </list>
/// Unpacked values: <see cref="int"/> for <c>b B h H i</c>, <see cref="uint"/> for <c>I</c>,
/// <see cref="float"/> for <c>f</c>, <see cref="bool"/> for <c>?</c>.
/// </summary>
public static class StructFormat
{
    /// <summary>Size in bytes (Python <c>struct.calcsize</c>).</summary>
    public static int CalcSize(string format) => Layout(format).Size;

    /// <summary>Byte offset of every item (Python has no direct equivalent; used for EMEVD parameters).</summary>
    public static IReadOnlyList<int> Offsets(string format) => Layout(format).Offsets;

    /// <summary>The item codes without the byte-order prefix.</summary>
    public static string Codes(string format) => StripPrefix(format ?? throw new ArgumentNullException(nameof(format))).Codes;

    public static object[] Unpack(string format, ReadOnlySpan<byte> data)
    {
        var layout = Layout(format);
        if (data.Length < layout.Size)
        {
            throw new FormatException($"Format '{format}' needs {layout.Size} bytes, got {data.Length}.");
        }

        var values = new object[layout.Codes.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var slice = data[layout.Offsets[i]..];
            values[i] = layout.Codes[i] switch
            {
                'b' => (int)(sbyte)slice[0],
                'B' => (int)slice[0],
                '?' => slice[0] != 0,
                'h' => (int)(layout.BigEndian ? BinaryPrimitives.ReadInt16BigEndian(slice) : BinaryPrimitives.ReadInt16LittleEndian(slice)),
                'H' => (int)(layout.BigEndian ? BinaryPrimitives.ReadUInt16BigEndian(slice) : BinaryPrimitives.ReadUInt16LittleEndian(slice)),
                'i' => layout.BigEndian ? BinaryPrimitives.ReadInt32BigEndian(slice) : BinaryPrimitives.ReadInt32LittleEndian(slice),
                'I' => layout.BigEndian ? BinaryPrimitives.ReadUInt32BigEndian(slice) : BinaryPrimitives.ReadUInt32LittleEndian(slice),
                'f' => layout.BigEndian ? BinaryPrimitives.ReadSingleBigEndian(slice) : BinaryPrimitives.ReadSingleLittleEndian(slice),
                var c => throw new FormatException($"Unsupported struct code '{c}'."),
            };
        }

        return values;
    }

    /// <summary>Packs <paramref name="values"/> (any numeric type; range checked like Python).</summary>
    public static byte[] Pack(string format, IReadOnlyList<object> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var layout = Layout(format);
        if (values.Count != layout.Codes.Length)
        {
            throw new ArgumentException($"Format '{format}' needs {layout.Codes.Length} values, got {values.Count}.", nameof(values));
        }

        var data = new byte[layout.Size];
        for (var i = 0; i < values.Count; i++)
        {
            var slice = data.AsSpan(layout.Offsets[i]);
            var value = values[i];
            switch (layout.Codes[i])
            {
                case 'b': slice[0] = unchecked((byte)checked((sbyte)ToLong(value))); break;
                case 'B': slice[0] = checked((byte)ToLong(value)); break;
                case '?': slice[0] = value is bool b ? (byte)(b ? 1 : 0) : (byte)(ToLong(value) != 0 ? 1 : 0); break;
                case 'h': Write16(slice, checked((short)ToLong(value)), layout.BigEndian); break;
                case 'H': Write16(slice, unchecked((short)checked((ushort)ToLong(value))), layout.BigEndian); break;
                case 'i': Write32(slice, checked((int)ToLong(value)), layout.BigEndian); break;
                case 'I': Write32(slice, unchecked((int)checked((uint)ToLong(value))), layout.BigEndian); break;
                case 'f':
                    var f = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                    if (layout.BigEndian)
                    {
                        BinaryPrimitives.WriteSingleBigEndian(slice, f);
                    }
                    else
                    {
                        BinaryPrimitives.WriteSingleLittleEndian(slice, f);
                    }

                    break;
                default: throw new FormatException($"Unsupported struct code '{layout.Codes[i]}'.");
            }
        }

        return data;
    }

    private static long ToLong(object value) => value switch
    {
        bool b => b ? 1 : 0,
        float or double => throw new ArgumentException($"Integer expected, got float {value}."),
        _ => Convert.ToInt64(value, CultureInfo.InvariantCulture),
    };

    private static void Write16(Span<byte> span, short value, bool bigEndian)
    {
        if (bigEndian)
        {
            BinaryPrimitives.WriteInt16BigEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteInt16LittleEndian(span, value);
        }
    }

    private static void Write32(Span<byte> span, int value, bool bigEndian)
    {
        if (bigEndian)
        {
            BinaryPrimitives.WriteInt32BigEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteInt32LittleEndian(span, value);
        }
    }

    private static int SizeOf(char code) => code switch
    {
        'b' or 'B' or '?' => 1,
        'h' or 'H' => 2,
        'i' or 'I' or 'f' => 4,
        _ => throw new FormatException($"Unsupported struct code '{code}'."),
    };

    private static (string Codes, bool Native, bool BigEndian) StripPrefix(string format) =>
        format.Length > 0 && format[0] is '@' or '<' or '>' or '='
            ? (format[1..], format[0] == '@', format[0] == '>')
            : (format, true, false);

    private static StructLayout Layout(string format)
    {
        ArgumentNullException.ThrowIfNull(format);
        var (codes, native, bigEndian) = StripPrefix(format);
        var offsets = new int[codes.Length];
        var position = 0;
        for (var i = 0; i < codes.Length; i++)
        {
            var size = SizeOf(codes[i]);
            if (native && position % size != 0)
            {
                position += size - (position % size);
            }

            offsets[i] = position;
            position += size;
        }

        return new StructLayout(codes, offsets, position, bigEndian);
    }

    private sealed record StructLayout(string Codes, int[] Offsets, int Size, bool BigEndian);
}
