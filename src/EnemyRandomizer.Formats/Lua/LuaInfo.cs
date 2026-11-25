using System.Buffers.Binary;
using EnemyRandomizer.Formats.Binary;

namespace EnemyRandomizer.Formats.Lua;

/// <summary>One NPC AI entry; function names are referenced by index into <see cref="LuaInfo.FunctionNames"/> (-1 = none).</summary>
public sealed class LuaInfoEntry
{
    public uint NpcAi { get; set; }
    public int PrimaryRef { get; set; } = -1;
    public int SecondaryRef { get; set; } = -1;
    public byte FlagA { get; set; }
    public byte FlagB { get; set; }
    public byte FlagC { get; set; }
    public byte FlagD { get; set; }

    /// <summary>Shift references after a name was inserted after position <paramref name="index"/>.</summary>
    internal void UpdateAddRefs(int index)
    {
        if (index < PrimaryRef)
        {
            PrimaryRef++;
        }

        if (index < SecondaryRef)
        {
            SecondaryRef++;
        }
    }
}

/// <summary>Values for <see cref="LuaInfo.AddEntryAuto"/> (one row of <c>airef.csv</c> info data).</summary>
public sealed record LuaInfoValue(uint NpcAi, byte[] Primary, byte[] Secondary, byte FlagA, byte FlagB, byte FlagC, byte FlagD);

/// <summary>
/// AI info table (<c>.luainfo</c>): <c>LUAI</c>, three ints, 16-byte entries, then a string pool.
/// Legacy <c>luainfo_io.py</c>.
/// </summary>
public sealed class LuaInfo
{
    private const int HeaderSize = 16;
    private const int EntrySize = 16;

    public uint Unknown { get; set; }
    public uint Separator { get; set; }
    public List<LuaInfoEntry> Entries { get; } = [];
    public List<byte[]> FunctionNames { get; } = [];

    public static LuaInfo Read(ReadOnlySpan<byte> data)
    {
        if (!data.StartsWith("LUAI"u8))
        {
            throw new InvalidDataException("Expected 'LUAI' at offset 0.");
        }

        var info = new LuaInfo
        {
            Unknown = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]),
            Separator = BinaryPrimitives.ReadUInt32LittleEndian(data[12..]),
        };
        var count = BinaryPrimitives.ReadInt32LittleEndian(data[8..]);

        var offsets = new List<(uint Primary, uint Secondary)>();
        var position = HeaderSize;
        for (var i = 0; i < count; i++, position += EntrySize)
        {
            var row = data[position..];
            info.Entries.Add(new LuaInfoEntry
            {
                NpcAi = BinaryPrimitives.ReadUInt32LittleEndian(row),
                FlagA = row[12],
                FlagB = row[13],
                FlagC = row[14],
                FlagD = row[15],
            });
            offsets.Add((BinaryPrimitives.ReadUInt32LittleEndian(row[4..]), BinaryPrimitives.ReadUInt32LittleEndian(row[8..])));
        }

        var nameOffsets = new List<int>();
        while (position < data.Length)
        {
            var name = GameText.CString(data, position);
            nameOffsets.Add(position);
            info.FunctionNames.Add(name.ToArray());
            position += name.Length + 1;
        }

        for (var i = 0; i < info.Entries.Count; i++)
        {
            info.Entries[i].PrimaryRef = nameOffsets.IndexOf((int)offsets[i].Primary);
            info.Entries[i].SecondaryRef = nameOffsets.IndexOf((int)offsets[i].Secondary);
        }

        return info;
    }

    public byte[] Write()
    {
        var nameOffsets = NameOffsets();
        using var stream = new MemoryStream();
        Span<byte> buffer = stackalloc byte[4];

        stream.Write("LUAI"u8);
        WriteUInt(stream, buffer, Unknown);
        WriteUInt(stream, buffer, (uint)Entries.Count);
        WriteUInt(stream, buffer, Separator);
        foreach (var entry in Entries)
        {
            WriteUInt(stream, buffer, entry.NpcAi);
            WriteUInt(stream, buffer, entry.PrimaryRef > -1 ? (uint)nameOffsets[entry.PrimaryRef] : 0);
            WriteUInt(stream, buffer, entry.SecondaryRef > -1 ? (uint)nameOffsets[entry.SecondaryRef] : 0);
            stream.WriteByte(entry.FlagA);
            stream.WriteByte(entry.FlagB);
            stream.WriteByte(entry.FlagC);
            stream.WriteByte(entry.FlagD);
        }

        foreach (var name in FunctionNames)
        {
            stream.Write(name);
            stream.WriteByte(0);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Adds AI entries at the start unless an entry for the first value's NPC AI exists
    /// (legacy <c>AddEntryAuto</c>, behaviour kept as is). Returns true when it already existed.
    /// </summary>
    public bool AddEntryAuto(IReadOnlyList<LuaInfoValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0 || Entries.Any(e => e.NpcAi == values[0].NpcAi))
        {
            return values.Count > 0;
        }

        foreach (var value in values)
        {
            var hasPrimary = value.Primary.Length > 0;
            var hasSecondary = value.Secondary.Length > 0;
            if (hasPrimary)
            {
                FunctionNames.Insert(0, value.Primary);
                Entries.ForEach(e => e.UpdateAddRefs(-1));
            }

            if (hasSecondary)
            {
                FunctionNames.Insert(1, value.Secondary);
                Entries.ForEach(e => e.UpdateAddRefs(0));
            }

            Entries.Insert(0, new LuaInfoEntry
            {
                NpcAi = value.NpcAi,
                FlagA = value.FlagA,
                FlagB = value.FlagB,
                FlagC = value.FlagC,
                FlagD = value.FlagD,
                PrimaryRef = hasPrimary ? 0 : -1,
                SecondaryRef = hasSecondary ? 1 : -1,
            });
        }

        return false;
    }

    private List<int> NameOffsets()
    {
        var offsets = new List<int>(FunctionNames.Count);
        var position = HeaderSize + (Entries.Count * EntrySize);
        foreach (var name in FunctionNames)
        {
            offsets.Add(position);
            position += name.Length + 1;
        }

        return offsets;
    }

    private static void WriteUInt(Stream stream, Span<byte> buffer, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        stream.Write(buffer);
    }
}
