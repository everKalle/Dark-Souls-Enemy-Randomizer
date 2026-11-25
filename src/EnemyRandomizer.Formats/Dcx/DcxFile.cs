using System.Buffers.Binary;
using System.IO.Compression;

namespace EnemyRandomizer.Formats.Dcx;

/// <summary>Header values of a DCX container that are written back unchanged.</summary>
public sealed record DcxHeader(
    uint Req1,
    uint Req2,
    uint Req3,
    uint Req4,
    uint HeaderLength,
    byte[] UnknownHeaderPart,
    uint CompressedHeaderLength)
{
    private const int UnknownPartLength = 0x18;

    /// <summary>Header used for EMEVD files written from scratch (legacy <c>set_emevd_dcx_values</c>).</summary>
    public static DcxHeader Emevd { get; } = new(
        0x100, 0x18, 0x24, 0x24, 44,
        [0, 0, 0, 0x20, 0x09, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0],
        8);

    internal static int UnknownLength => UnknownPartLength;
}

/// <summary>
/// DCX "DFLT" container (Remastered). Mixed endianness: the version is little-endian, everything else big-endian.
/// Body = 2-byte zlib header + raw deflate (legacy <c>dcx_handler.py</c>).
/// </summary>
public static class DcxFile
{
    private static ReadOnlySpan<byte> Dcx => "DCX\0"u8;
    private static ReadOnlySpan<byte> Dcs => "DCS\0"u8;
    private static ReadOnlySpan<byte> DcpDflt => "DCP\0DFLT"u8;
    private static ReadOnlySpan<byte> Dca => "DCA\0"u8;

    public static bool IsDcx(ReadOnlySpan<byte> data) => data.StartsWith(Dcx);

    /// <summary>Returns the header and the decompressed content.</summary>
    public static (DcxHeader Header, byte[] Data) Read(ReadOnlySpan<byte> data)
    {
        Expect(data, 0, Dcx, "DCX");
        var req1 = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        var req2 = BinaryPrimitives.ReadUInt32BigEndian(data[8..]);
        var req3 = BinaryPrimitives.ReadUInt32BigEndian(data[12..]);
        var req4 = BinaryPrimitives.ReadUInt32BigEndian(data[16..]);
        if (req1 != 0x100 || req2 != 0x18 || req3 != 0x24 || req4 != 0x24)
        {
            throw new InvalidDataException($"Unexpected DCX header values {req1:X}/{req2:X}/{req3:X}/{req4:X}.");
        }

        var headerLength = BinaryPrimitives.ReadUInt32BigEndian(data[20..]);
        Expect(data, 24, Dcs, "DCS");
        var uncompressedSize = (int)BinaryPrimitives.ReadUInt32BigEndian(data[28..]);
        var compressedSize = (int)BinaryPrimitives.ReadUInt32BigEndian(data[32..]);
        Expect(data, 36, DcpDflt, "DCP DFLT");
        var unknown = data.Slice(44, DcxHeader.UnknownLength).ToArray();
        Expect(data, 68, Dca, "DCA");
        var compressedHeaderLength = BinaryPrimitives.ReadUInt32BigEndian(data[72..]);

        const int bodyStart = 76 + 2; // skip the 2-byte zlib header, the rest is raw deflate
        var body = data.Slice(bodyStart, Math.Min(compressedSize - 2, data.Length - bodyStart)).ToArray();

        var output = new byte[uncompressedSize];
        using (var deflate = new DeflateStream(new MemoryStream(body), CompressionMode.Decompress))
        {
            deflate.ReadExactly(output);
        }

        var header = new DcxHeader(req1, req2, req3, req4, headerLength, unknown, compressedHeaderLength);
        return (header, output);
    }

    /// <summary>Compresses <paramref name="content"/> (zlib, best compression) into a DCX container.</summary>
    public static byte[] Write(DcxHeader header, ReadOnlySpan<byte> content)
    {
        ArgumentNullException.ThrowIfNull(header);
        if (header.UnknownHeaderPart.Length != DcxHeader.UnknownLength)
        {
            throw new ArgumentException($"Unknown header part must be {DcxHeader.UnknownLength} bytes.", nameof(header));
        }

        byte[] compressed;
        using (var buffer = new MemoryStream())
        {
            using (var zlib = new ZLibStream(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                zlib.Write(content);
            }

            compressed = buffer.ToArray();
        }

        var output = new byte[76 + compressed.Length];
        var span = output.AsSpan();
        Dcx.CopyTo(span);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], header.Req1);
        BinaryPrimitives.WriteUInt32BigEndian(span[8..], header.Req2);
        BinaryPrimitives.WriteUInt32BigEndian(span[12..], header.Req3);
        BinaryPrimitives.WriteUInt32BigEndian(span[16..], header.Req4);
        BinaryPrimitives.WriteUInt32BigEndian(span[20..], header.HeaderLength);
        Dcs.CopyTo(span[24..]);
        BinaryPrimitives.WriteUInt32BigEndian(span[28..], (uint)content.Length);
        BinaryPrimitives.WriteUInt32BigEndian(span[32..], (uint)compressed.Length);
        DcpDflt.CopyTo(span[36..]);
        header.UnknownHeaderPart.CopyTo(span[44..]);
        Dca.CopyTo(span[68..]);
        BinaryPrimitives.WriteUInt32BigEndian(span[72..], header.CompressedHeaderLength);
        compressed.CopyTo(span[76..]);
        return output;
    }

    private static void Expect(ReadOnlySpan<byte> data, int offset, ReadOnlySpan<byte> expected, string name)
    {
        if (data.Length < offset + expected.Length || !data.Slice(offset, expected.Length).SequenceEqual(expected))
        {
            throw new InvalidDataException($"Expected '{name}' at offset {offset}.");
        }
    }
}
