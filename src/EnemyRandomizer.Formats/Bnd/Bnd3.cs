using System.Buffers.Binary;
using EnemyRandomizer.Formats.Binary;

namespace EnemyRandomizer.Formats.Bnd;

/// <summary>One file inside a BND3 archive. <see cref="Name"/> is Shift-JIS encoded on disk.</summary>
public sealed record BndEntry(int Id, string Name, byte[] Data);

/// <summary>
/// BND3 archive (<c>.luabnd</c>, <c>.parambnd</c>, <c>.ffxbnd</c>). Implementation by HotPocketRemix,
/// legacy <c>bnd_rebuilder.py</c>. Writing always uses version <c>07D7R6</c>, flag 0x74 and 16-byte data alignment.
/// </summary>
public static class Bnd3
{
    private const int HeaderSize = 0x20;
    private const int RecordSeparator = 0x40;
    private static ReadOnlySpan<byte> Magic => "BND3"u8;
    private static ReadOnlySpan<byte> Version => "BND307D7R6\0\0"u8;

    public static bool IsBnd3(ReadOnlySpan<byte> data) => data.StartsWith(Magic);

    public static List<BndEntry> Read(ReadOnlySpan<byte> data)
    {
        if (!IsBnd3(data))
        {
            throw new InvalidDataException("Expected 'BND3' at offset 0.");
        }

        if (data.Length < HeaderSize)
        {
            throw new InvalidDataException("BND3 header is truncated.");
        }

        var flag = BinaryPrimitives.ReadUInt32LittleEndian(data[0x0C..]);
        var count = BinaryPrimitives.ReadInt32LittleEndian(data[0x10..]);
        var recordSize = flag switch
        {
            0x74 or 0x54 => 24,
            0x70 => 20,
            _ => throw new InvalidDataException($"Unknown BND3 flag 0x{flag:X}."),
        };

        if (count < 0 || HeaderSize + ((long)count * recordSize) > data.Length)
        {
            throw new InvalidDataException($"BND3 entry count {count} does not fit into the file.");
        }

        var entries = new List<BndEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var record = data[(HeaderSize + (i * recordSize))..];
            var separator = BinaryPrimitives.ReadUInt32LittleEndian(record);
            if (separator != RecordSeparator)
            {
                throw new InvalidDataException($"Unknown BND3 record separator 0x{separator:X}.");
            }

            var size = BinaryPrimitives.ReadInt32LittleEndian(record[4..]);
            var offset = BinaryPrimitives.ReadInt32LittleEndian(record[8..]);
            var id = BinaryPrimitives.ReadInt32LittleEndian(record[12..]);
            var nameOffset = BinaryPrimitives.ReadInt32LittleEndian(record[16..]);

            if (size < 0 || offset < 0 || (long)offset + size > data.Length || nameOffset < 0)
            {
                throw new InvalidDataException($"BND3 entry {i} points outside of the file.");
            }

            var name = GameText.Decode(GameText.CString(data, nameOffset));
            entries.Add(new BndEntry(id, name, data.Slice(offset, size).ToArray()));
        }

        return entries;
    }

    public static byte[] Write(IReadOnlyList<BndEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        const int recordSize = 24;

        var names = entries.Select(e => GameText.Encode(e.Name)).ToList();
        var nameOffset = HeaderSize + (recordSize * entries.Count);
        var nameEnd = nameOffset + names.Sum(n => n.Length + 1);

        // Data offsets: each entry padded to 16 bytes.
        var dataOffsets = new int[entries.Count];
        var position = nameEnd;
        for (var i = 0; i < entries.Count; i++)
        {
            position = GameText.Align(position, 16);
            dataOffsets[i] = position;
            position += entries[i].Data.Length;
        }

        var output = new byte[position];
        var span = output.AsSpan();
        Version.CopyTo(span);
        BinaryPrimitives.WriteUInt32LittleEndian(span[0x0C..], 0x74);
        BinaryPrimitives.WriteInt32LittleEndian(span[0x10..], entries.Count);
        BinaryPrimitives.WriteInt32LittleEndian(span[0x14..], nameEnd);

        for (var i = 0; i < entries.Count; i++)
        {
            var record = span[(HeaderSize + (i * recordSize))..];
            BinaryPrimitives.WriteInt32LittleEndian(record, RecordSeparator);
            BinaryPrimitives.WriteInt32LittleEndian(record[4..], entries[i].Data.Length);
            BinaryPrimitives.WriteInt32LittleEndian(record[8..], dataOffsets[i]);
            BinaryPrimitives.WriteInt32LittleEndian(record[12..], entries[i].Id);
            BinaryPrimitives.WriteInt32LittleEndian(record[16..], nameOffset);
            BinaryPrimitives.WriteInt32LittleEndian(record[20..], entries[i].Data.Length);

            names[i].CopyTo(span[nameOffset..]);
            nameOffset += names[i].Length + 1;
            entries[i].Data.CopyTo(span[dataOffsets[i]..]);
        }

        return output;
    }
}
