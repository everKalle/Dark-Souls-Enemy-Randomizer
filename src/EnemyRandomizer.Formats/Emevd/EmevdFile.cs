using System.Buffers.Binary;
using EnemyRandomizer.Formats.Binary;

namespace EnemyRandomizer.Formats.Emevd;

/// <summary>
/// Dark Souls event script (<c>.emevd</c>, magic <c>EVD\0</c>, 0xCC). Legacy <c>emevd_handler.py</c>.
/// Layout: 84-byte header, event table (28 bytes each), instruction table (24), argument data, parameter table (20).
/// </summary>
public sealed class EmevdFile
{
    private const int HeaderSize = 84;
    private const int EventSize = 28;
    private const int InstructionSize = 24;
    private const int ParameterSize = 20;

    public List<EmevdEvent> Events { get; } = [];

    /// <summary>Reads an EMEVD; any structural problem (offsets outside the file) is an <see cref="InvalidDataException"/>.</summary>
    public static EmevdFile Read(ReadOnlySpan<byte> data, InstructionCatalog? catalog = null)
    {
        try
        {
            return ReadCore(data, catalog ?? InstructionCatalog.Default);
        }
        catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException or OverflowException or KeyNotFoundException or InvalidCastException)
        {
            throw new InvalidDataException("The EMEVD file is malformed: " + e.Message, e);
        }
    }

    private static EmevdFile ReadCore(ReadOnlySpan<byte> data, InstructionCatalog catalog)
    {
        if (data.Length < 0x48 || !data.StartsWith("EVD\0"u8))
        {
            throw new InvalidDataException("Expected 'EVD' at offset 0.");
        }

        if (data[8] != 0xCC)
        {
            throw new InvalidDataException($"Expected magic const 0xCC, got 0x{data[8]:X}.");
        }

        var header = data[0x0C..];
        var eventCount = HeaderInt(header, 1);
        var eventTable = HeaderInt(header, 2);
        var instructionTable = HeaderInt(header, 4);
        var parameterTable = HeaderInt(header, 10);
        var argumentOffset = HeaderInt(header, 14);

        var file = new EmevdFile();
        for (var e = 0; e < eventCount; e++)
        {
            var row = data[(eventTable + (e * EventSize))..];
            var ev = new EmevdEvent
            {
                Id = BinaryPrimitives.ReadUInt32LittleEndian(row),
                RestBehavior = BinaryPrimitives.ReadUInt32LittleEndian(row[20..]),
            };
            var instructionCount = BinaryPrimitives.ReadInt32LittleEndian(row[4..]);
            var instructionOffset = BinaryPrimitives.ReadInt32LittleEndian(row[8..]);
            var parameterCount = BinaryPrimitives.ReadInt32LittleEndian(row[12..]);
            var parameterOffset = BinaryPrimitives.ReadInt32LittleEndian(row[16..]);

            for (var i = 0; i < instructionCount; i++)
            {
                ev.Instructions.Add(ReadInstruction(data, instructionTable + instructionOffset + (i * InstructionSize), argumentOffset, catalog));
            }

            for (var p = 0; p < parameterCount; p++)
            {
                var prow = data[(parameterTable + parameterOffset + (p * ParameterSize))..];
                var instruction = ev.Instructions[BinaryPrimitives.ReadInt32LittleEndian(prow)];
                var parameter = new EmevdParameter
                {
                    DestinationStartByte = BinaryPrimitives.ReadInt32LittleEndian(prow[4..]),
                    SourceStartByte = BinaryPrimitives.ReadInt32LittleEndian(prow[8..]),
                    Length = BinaryPrimitives.ReadInt32LittleEndian(prow[12..]),
                };
                parameter.ArgumentType = instruction.ArgumentTypeAt(parameter.DestinationStartByte, parameter.Length);

                if (!ev.EventParameters.Any(x => x.Offset == parameter.SourceStartByte))
                {
                    ev.EventParameters.Add((parameter.SourceStartByte, parameter.ArgumentType));
                }

                instruction.Parameters.Add(parameter);
            }

            var sorted = ev.EventParameters.OrderBy(x => x.Offset).ToList();
            ev.EventParameters.Clear();
            ev.EventParameters.AddRange(sorted);
            file.Events.Add(ev);
        }

        file.ResolveInitEvents(catalog);
        return file;
    }

    public byte[] Write()
    {
        using var events = new MemoryStream();
        using var instructions = new MemoryStream();
        using var arguments = new MemoryStream();
        using var parameters = new MemoryStream();
        var instructionCount = 0;
        var parameterCount = 0;

        foreach (var ev in Events)
        {
            var instructionOffset = (int)instructions.Length;
            var parameterOffset = (int)parameters.Length;
            var eventParameterCount = 0;

            for (var i = 0; i < ev.Instructions.Count; i++)
            {
                var instruction = ev.Instructions[i];
                var argBytes = StructFormat.Pack(instruction.ArgumentFormat, instruction.Args);
                var padded = new byte[GameText.Align(argBytes.Length, 4)];
                argBytes.CopyTo(padded, 0);
                var argOffset = (int)arguments.Length;
                arguments.Write(padded);

                foreach (var parameter in instruction.Parameters)
                {
                    WriteInts(parameters, i, parameter.DestinationStartByte, parameter.SourceStartByte, parameter.Length, 0);
                    eventParameterCount++;
                }

                WriteInts(instructions, instruction.Class, instruction.Index, padded.Length, argOffset, instruction.SignedInt, 0);
            }

            WriteInts(events, (int)ev.Id, ev.Instructions.Count, instructionOffset, eventParameterCount,
                eventParameterCount == 0 ? -1 : parameterOffset, (int)ev.RestBehavior, 0);
            instructionCount += ev.Instructions.Count;
            parameterCount += eventParameterCount;
        }

        var instructionTable = HeaderSize + (int)events.Length;
        var argumentOffset = instructionTable + (int)instructions.Length;
        var argumentSize = (int)arguments.Length;
        var parameterTable = argumentOffset + argumentSize;
        var fileSize = parameterTable + (int)parameters.Length;

        using var output = new MemoryStream(fileSize);
        output.Write("EVD\0"u8);
        output.Write([0, 0, 0, 0, 0xCC, 0, 0, 0]);
        WriteInts(output, fileSize, Events.Count, HeaderSize, instructionCount, instructionTable, 0, argumentOffset, 0,
            argumentOffset, parameterCount, parameterTable, 0, fileSize, argumentSize, argumentOffset, 0, fileSize, 0);
        events.WriteTo(output);
        instructions.WriteTo(output);
        arguments.WriteTo(output);
        parameters.WriteTo(output);
        return output.ToArray();
    }

    /// <summary>Resolves <c>init_event</c> argument formats from the events defined in this file.</summary>
    internal void ResolveInitEvents(InstructionCatalog catalog)
    {
        var types = Events.GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.Last().ParameterTypes);
        foreach (var ev in Events)
        {
            ev.ResolveInitEventArguments(types, catalog);
        }
    }

    private static EmevdInstruction ReadInstruction(ReadOnlySpan<byte> data, int position, int argumentOffset, InstructionCatalog catalog)
    {
        var row = data[position..];
        var instruction = new EmevdInstruction
        {
            Class = BinaryPrimitives.ReadInt32LittleEndian(row),
            Index = BinaryPrimitives.ReadInt32LittleEndian(row[4..]),
            SignedInt = BinaryPrimitives.ReadInt32LittleEndian(row[16..]),
        };
        var argSize = BinaryPrimitives.ReadInt32LittleEndian(row[8..]);
        var argOffset = BinaryPrimitives.ReadInt32LittleEndian(row[12..]);
        instruction.ArgumentBytes = data.Slice(argumentOffset + argOffset, argSize).ToArray();
        instruction.ArgumentFormat = catalog.Format(instruction.Class, instruction.Index);

        if (!instruction.IsInitEvent)
        {
            instruction.Args = [.. StructFormat.Unpack(instruction.ArgumentFormat, instruction.ArgumentBytes)];
        }

        return instruction;
    }

    private static int HeaderInt(ReadOnlySpan<byte> header, int index) =>
        BinaryPrimitives.ReadInt32LittleEndian(header[(index * 4)..]);

    private static void WriteInts(Stream stream, params int[] values)
    {
        Span<byte> buffer = stackalloc byte[4];
        foreach (var value in values)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            stream.Write(buffer);
        }
    }
}
