using System.Buffers.Binary;
using EnemyRandomizer.Formats.Binary;

namespace EnemyRandomizer.Formats.Msb;

/// <summary>
/// Dark Souls map layout (<c>map/MapStudio/*.msb</c>): models, events, points (regions) and parts (map pieces,
/// objects, creatures, collisions...). Legacy <c>msb_io.py</c> (with help from Wulf2k's MSBEdit). Writing rebuilds
/// all offsets exactly like v0.4.x; points are written sorted by their index field.
/// </summary>
public sealed class MsbFile
{
    private static readonly int[] PointTypes = [0, 2, 3, 5];
    private static readonly int[] PartTypes = [0, 1, 2, 4, 5, 8, 9, 0xA, 0xB];

    public MsbTable Models { get; } = new("models");

    /// <summary>Event tables by event type 0..12.</summary>
    public IReadOnlyList<MsbTable> Events { get; } = [.. Enumerable.Range(0, 13).Select(i => new MsbTable("events" + i))];

    /// <summary>Point tables for point types 0, 2, 3, 5.</summary>
    public IReadOnlyList<MsbTable> Points { get; } = [.. PointTypes.Select(t => new MsbTable("points" + t))];

    /// <summary>Part tables: mapPieces0, objects1, creatures2, creatures4, collision5, navimesh8, objects9, creatures10, collision11.</summary>
    public IReadOnlyList<MsbTable> Parts { get; } =
    [
        new("mapPieces0"), new("objects1"), new("creatures2"), new("creatures4"), new("collision5"),
        new("navimesh8"), new("objects9"), new("creatures10"), new("collision11"),
    ];

    /// <summary>Enemies and NPCs (part type 2).</summary>
    public MsbTable Creatures => Parts[2];

    public static MsbFile Read(ReadOnlySpan<byte> data)
    {
        var msb = new MsbFile();
        const int modelPointer = 0;
        var modelCount = Int(data, 0x8);
        var eventPointer = Int(data, (modelCount * 4) + 0x8);
        var eventCount = Int(data, eventPointer + 0x8);
        var pointPointer = Int(data, (eventCount * 4) + 0x8 + eventPointer);
        var pointCount = Int(data, pointPointer + 0x8);
        var partsPointer = Int(data, (pointCount * 4) + 0x8 + pointPointer);
        var partsCount = Int(data, partsPointer + 0x8);

        for (var i = 0; i < modelCount - 1; i++)
        {
            ReadRow(data, Int(data, modelPointer + 0xC + (i * 4)), msb.Models);
        }

        for (var i = 0; i < eventCount - 1; i++)
        {
            var offset = Int(data, eventPointer + 0xC + (i * 4));
            ReadRow(data, offset, msb.Events[Int(data, offset + 0x8)]);
        }

        for (var i = 0; i < pointCount - 1; i++)
        {
            var offset = Int(data, pointPointer + 0xC + (i * 4));
            ReadRow(data, offset, msb.Points[IndexOfType(PointTypes, Int(data, offset + 0xC), "point")]);
        }

        for (var i = 0; i < partsCount - 1; i++)
        {
            var offset = Int(data, partsPointer + 0xC + (i * 4));
            ReadRow(data, offset, msb.Parts[IndexOfType(PartTypes, Int(data, offset + 0x4), "part")]);
        }

        return msb;
    }

    public byte[] Write()
    {
        var output = new GrowingBuffer();

        // Models
        var modelCount = Models.Rows.Count + 1;
        var modelParamOffset = 0xC + (modelCount * 4);
        output.WriteAt(0, StructFormat.Pack("<III", [0, modelParamOffset, modelCount]));
        output.WriteAt(modelParamOffset, GameText.Encode("MODEL_PARAM_ST"));
        var offset = NextAligned(output.Length);

        for (var i = 0; i < Models.Rows.Count; i++)
        {
            var current = offset;
            output.WriteInt(0xC + (i * 4), current);

            var row = Models.Rows[i];
            var layout = Models.Layout;
            var nameOffset = Convert.ToInt32(row[0], System.Globalization.CultureInfo.InvariantCulture);
            var name = GameText.Encode((string)row[layout.NameIndex]);
            var sib = GameText.Encode((string)row[layout.NameIndex + 1]);

            var padding = (sib.Length + name.Length + 5) & -4;
            if (padding <= 0x10)
            {
                padding = 0x14;
            }

            output.WriteAt(current, StructFormat.Pack(layout.BeforeName, row.Values[..layout.NameIndex]));
            output.WriteAt(current + nameOffset, [.. name, 0, .. sib, 0]);
            offset = current + nameOffset + padding;
        }

        // Events
        var eventPointer = NextAligned(output.Length);
        output.WriteInt(0xC + ((modelCount - 1) * 4), eventPointer);
        var eventCount = 1 + Events.Sum(t => t.Rows.Count);
        offset = WriteSectionHeader(output, eventPointer, eventCount, "EVENT_PARAM_ST");
        var index = 0;
        foreach (var table in Events)
        {
            foreach (var row in table.Rows)
            {
                offset = WriteRow(output, offset, eventPointer, row, index++);
            }
        }

        // Points (sorted by their index field, legacy behaviour)
        var pointPointer = output.Length;
        output.WriteInt(eventPointer + 0xC + ((eventCount - 1) * 4), pointPointer);
        var pointCount = 1 + Points.Sum(t => t.Rows.Count);
        offset = WriteSectionHeader(output, pointPointer, pointCount, "POINT_PARAM_ST");
        var sortedPoints = Points.SelectMany(t => t.Rows)
                                 .OrderBy(r => Convert.ToInt32(r[2], System.Globalization.CultureInfo.InvariantCulture))
                                 .ToList();
        for (var i = 0; i < sortedPoints.Count; i++)
        {
            offset = WriteRow(output, offset, pointPointer, sortedPoints[i], i);
        }

        // Parts
        var partsPointer = output.Length;
        output.WriteInt(pointPointer + 0xC + ((pointCount - 1) * 4), partsPointer);
        var partsCount = 1 + Parts.Sum(t => t.Rows.Count);
        offset = WriteSectionHeader(output, partsPointer, partsCount, "PARTS_PARAM_ST");
        index = 0;
        foreach (var table in Parts)
        {
            foreach (var row in table.Rows)
            {
                offset = WriteRow(output, offset, partsPointer, row, index++);
            }
        }

        return output.ToArray();
    }

    /// <summary>
    /// Appends a creature and shifts every part reference that points at or after its new part index
    /// (legacy <c>AddCreatureRow</c>).
    /// </summary>
    public void AddCreature(MsbRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        Creatures.Rows.Add(row);
        var newPartIndex = Parts[2].Rows.Count + Parts[1].Rows.Count + Parts[0].Rows.Count - 1;
        foreach (var table in Events.Concat(Points).Concat(Parts))
        {
            table.UpdatePartIndices(newPartIndex);
        }
    }

    private static void ReadRow(ReadOnlySpan<byte> data, int pointer, MsbTable table)
    {
        var layout = table.Layout;
        var values = new object[layout.Fields.Count];
        Array.Fill(values, 0);

        var nameOffset = Int(data, pointer);
        var nameBytes = GameText.CString(data, pointer + nameOffset);
        values[layout.NameIndex] = GameText.Decode(nameBytes);
        var stringLength = nameBytes.Length + 1;

        if (layout.HasSib)
        {
            var sibOffset = table.Layout.Name == "models" ? nameOffset + nameBytes.Length + 1 : Int(data, pointer + 0x10);
            var sibBytes = GameText.CString(data, pointer + sibOffset);
            values[layout.NameIndex + 1] = GameText.Decode(sibBytes);
            stringLength += sibBytes.Length + 1;
            if (sibBytes.Length == 0)
            {
                stringLength += 5;
            }
        }

        stringLength = (stringLength + 3) & -4;

        var before = StructFormat.Unpack(layout.BeforeName, data[pointer..]);
        before.CopyTo(values, 0);

        if (layout.AfterName.Length > 1)
        {
            var after = StructFormat.Unpack(layout.AfterName, data[(pointer + nameOffset + stringLength)..]);
            after.CopyTo(values, layout.AfterNameIndex);
        }

        table.Rows.Add(new MsbRow(layout, values));
    }

    private static int WriteRow(GrowingBuffer output, int position, int pointer, MsbRow row, int index)
    {
        var layout = row.Layout;
        output.WriteInt(pointer + 0xC + (index * 4), position);

        var nameOffset = Convert.ToInt32(row[0], System.Globalization.CultureInfo.InvariantCulture);
        var name = GameText.Encode((string)row[layout.NameIndex]);
        var stringLength = name.Length + 1;
        byte[] sib = [];
        if (layout.HasSib)
        {
            sib = GameText.Encode((string)row[layout.NameIndex + 1]);
            stringLength += sib.Length + 1;
            if (sib.Length == 0)
            {
                stringLength += 5;
            }
        }

        stringLength = (stringLength + 3) & -4;

        output.WriteAt(position, StructFormat.Pack(layout.BeforeName, row.Values[..layout.NameIndex]));
        var offset = output.WriteAt(position + nameOffset, [.. name, 0]);
        if (layout.HasSib)
        {
            output.WriteAt(offset, [.. sib, 0]);
        }

        offset = position + nameOffset + stringLength;
        if (layout.AfterName.Length > 1)
        {
            var count = StructFormat.Codes(layout.AfterName).Length;
            offset = output.WriteAt(offset, StructFormat.Pack(layout.AfterName, row.Values[layout.AfterNameIndex..(layout.AfterNameIndex + count)]));
        }

        return offset;
    }

    private static int WriteSectionHeader(GrowingBuffer output, int pointer, int count, string name)
    {
        var nameOffset = pointer + 0xC + (count * 4);
        output.WriteAt(pointer, StructFormat.Pack("<III", [0, nameOffset, count]));
        output.WriteAt(nameOffset, GameText.Encode(name));
        return NextAligned(output.Length);
    }

    /// <summary>Legacy <c>(len &amp; -4) + 4</c>: always moves forward, at least one zero byte after a string.</summary>
    private static int NextAligned(int length) => (length & -4) + 4;

    private static int IndexOfType(int[] types, int type, string what)
    {
        var index = Array.IndexOf(types, type);
        return index >= 0 ? index : throw new InvalidDataException($"Unknown MSB {what} type {type}.");
    }

    private static int Int(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);

    /// <summary>Byte buffer that grows with zeros when writing past its end (legacy <c>WriteBytesAt</c>).</summary>
    private sealed class GrowingBuffer
    {
        private byte[] _buffer = new byte[1 << 16];

        public int Length { get; private set; }

        public int WriteAt(int position, ReadOnlySpan<byte> data)
        {
            var end = position + data.Length;
            if (end > _buffer.Length)
            {
                Array.Resize(ref _buffer, Math.Max(end, _buffer.Length * 2));
            }

            data.CopyTo(_buffer.AsSpan(position));
            Length = Math.Max(Length, end);
            return end;
        }

        public void WriteInt(int position, int value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            WriteAt(position, bytes);
        }

        public byte[] ToArray() => _buffer.AsSpan(0, Length).ToArray();
    }
}
