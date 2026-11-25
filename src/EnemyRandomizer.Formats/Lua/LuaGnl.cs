using System.Buffers.Binary;
using EnemyRandomizer.Formats.Binary;

namespace EnemyRandomizer.Formats.Lua;

/// <summary>
/// List of global AI function names (<c>.luagnl</c>): a 0-terminated table of offsets, then one zero-terminated
/// name per offset entry. Legacy <c>luagnl_io.py</c> (format described by Meowmaritus).
/// </summary>
public sealed class LuaGnl
{
    private readonly List<uint> _offsets = [];

    public List<byte[]> FunctionNames { get; } = [];

    public IReadOnlyList<uint> Offsets => _offsets;

    public static LuaGnl Read(ReadOnlySpan<byte> data)
    {
        var gnl = new LuaGnl();
        var position = 0;
        uint value;
        do
        {
            value = BinaryPrimitives.ReadUInt32LittleEndian(data[position..]);
            gnl._offsets.Add(value);
            position += 4;
        }
        while (value != 0);

        for (var i = 0; i < gnl._offsets.Count; i++)
        {
            var name = GameText.CString(data, position);
            gnl.FunctionNames.Add(name.ToArray());
            position += name.Length + 1;
        }

        return gnl;
    }

    public byte[] Write()
    {
        using var stream = new MemoryStream();
        Span<byte> buffer = stackalloc byte[4];
        foreach (var offset in _offsets)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, offset);
            stream.Write(buffer);
        }

        foreach (var name in FunctionNames)
        {
            stream.Write(name);
            stream.WriteByte(0);
        }

        return stream.ToArray();
    }

    /// <summary>Inserts <paramref name="names"/> at the beginning (legacy <c>AddEntriesAuto</c>).</summary>
    public void AddEntriesAuto(IEnumerable<byte[]> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        FunctionNames.InsertRange(0, names);
        UpdateOffsets();
    }

    /// <summary>Recomputes the offset table; the last entry is always the terminating 0.</summary>
    public void UpdateOffsets()
    {
        _offsets.Clear();
        var baseOffset = (uint)(FunctionNames.Count * 4);
        for (var i = 0; i < FunctionNames.Count; i++)
        {
            if (i == FunctionNames.Count - 1)
            {
                _offsets.Add(0);
            }
            else
            {
                _offsets.Add(baseOffset);
                baseOffset += (uint)FunctionNames[i].Length + 1;
            }
        }
    }
}
