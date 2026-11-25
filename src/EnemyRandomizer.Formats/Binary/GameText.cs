using System.Text;

namespace EnemyRandomizer.Formats.Binary;

/// <summary>Shift-JIS text used by all FromSoftware formats (legacy <c>byteread.py</c>).</summary>
public static class GameText
{
    private static readonly Lazy<Encoding> Encoding = new(() =>
    {
        System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return System.Text.Encoding.GetEncoding(932);
    });

    public static Encoding ShiftJis => Encoding.Value;

    public static byte[] Encode(string text) => ShiftJis.GetBytes(text ?? "");

    public static string Decode(ReadOnlySpan<byte> bytes) => ShiftJis.GetString(bytes);

    /// <summary>Bytes of the zero terminated string at <paramref name="offset"/> (without the terminator).</summary>
    public static ReadOnlySpan<byte> CString(ReadOnlySpan<byte> data, int offset)
    {
        if (offset >= data.Length)
        {
            return [];
        }

        var end = data[offset..].IndexOf((byte)0);
        return end < 0 ? data[offset..] : data.Slice(offset, end);
    }

    /// <summary>Rounds <paramref name="value"/> up to a multiple of <paramref name="alignment"/>.</summary>
    public static int Align(int value, int alignment) =>
        alignment == 0 || value % alignment == 0 ? value : value + alignment - (value % alignment);
}
