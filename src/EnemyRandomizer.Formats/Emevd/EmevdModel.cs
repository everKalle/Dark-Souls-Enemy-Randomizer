using EnemyRandomizer.Formats.Binary;

namespace EnemyRandomizer.Formats.Emevd;

/// <summary>Copies an event parameter (bytes of the event's own arguments) into an instruction argument.</summary>
public sealed class EmevdParameter
{
    public int DestinationStartByte { get; set; }
    public int SourceStartByte { get; set; }
    public int Length { get; set; }
    public char ArgumentType { get; set; }

    public EmevdParameter Clone() => (EmevdParameter)MemberwiseClone();
}

/// <summary>One instruction call: <c>class[index](args)</c>.</summary>
public sealed class EmevdInstruction
{
    /// <summary>Class/index of <c>init_event</c>, whose argument format depends on the started event.</summary>
    public const int InitEventClass = 2000;

    public int Class { get; set; }
    public int Index { get; set; }
    public int SignedInt { get; set; } = -1;

    /// <summary>Python <c>struct</c> format of the arguments (native alignment, e.g. <c>@iB</c>).</summary>
    public string ArgumentFormat { get; set; } = "@";

    /// <summary>Argument values: <see cref="int"/>, <see cref="uint"/> or <see cref="float"/>.</summary>
    public List<object> Args { get; set; } = [];

    public List<EmevdParameter> Parameters { get; } = [];

    /// <summary>Raw argument bytes as read (needed to re-parse <c>init_event</c>).</summary>
    internal byte[] ArgumentBytes { get; set; } = [];

    public bool IsInitEvent => Class == InitEventClass && Index == 0;

    public static EmevdInstruction Create(int instructionClass, int index, string format, params object[] args) =>
        new() { Class = instructionClass, Index = index, ArgumentFormat = format, Args = [.. args] };

    /// <summary>
    /// Index of the argument that ends at <paramref name="offset"/> + <paramref name="size"/>, -1 when none
    /// (legacy <c>arg_index_at_offset</c>).
    /// </summary>
    public int ArgumentIndexAt(int offset, int size)
    {
        var codes = StructFormat.Codes(ArgumentFormat);
        for (var k = 0; k <= codes.Length; k++)
        {
            if (StructFormat.CalcSize("@" + codes[..k]) - size == offset)
            {
                return k - 1;
            }
        }

        return -1;
    }

    /// <summary>Type code of the argument at a parameter destination (legacy <c>arg_type_at_offset</c>).</summary>
    public char ArgumentTypeAt(int offset, int size)
    {
        var index = ArgumentIndexAt(offset, size);
        if (index == -1)
        {
            throw new InvalidDataException($"Invalid offset for [{Class}, {Index}]: {offset}, {size}. Argument types are: {ArgumentFormat}");
        }

        var type = StructFormat.Codes(ArgumentFormat)[index];
        if (StructFormat.CalcSize("@" + type) != size)
        {
            throw new InvalidDataException($"Invalid type for [{Class}, {Index}]: {type} at offset {offset}. Expected size {size}.");
        }

        return type;
    }

    /// <summary>Deep copy (templates are added to several maps; legacy code shared one instance).</summary>
    public EmevdInstruction Clone()
    {
        var clone = new EmevdInstruction
        {
            Class = Class,
            Index = Index,
            SignedInt = SignedInt,
            ArgumentFormat = ArgumentFormat,
            Args = [.. Args],
            ArgumentBytes = ArgumentBytes,
        };
        clone.Parameters.AddRange(Parameters.Select(p => p.Clone()));
        return clone;
    }
}

/// <summary>One event: id, rest behaviour and instructions.</summary>
public sealed class EmevdEvent
{
    public uint Id { get; set; }

    /// <summary>Rest behaviour (0, 1 or 2; legacy name <c>unknown</c>).</summary>
    public uint RestBehavior { get; set; }

    public List<EmevdInstruction> Instructions { get; } = [];

    /// <summary>The event's own arguments: byte offset and type code, sorted by offset.</summary>
    public List<(int Offset, char Type)> EventParameters { get; } = [];

    /// <summary>Concatenated type codes of <see cref="EventParameters"/> (legacy <c>event_param_string</c>).</summary>
    public string ParameterTypes => string.Concat(EventParameters.Select(p => p.Type));

    public EmevdEvent Clone()
    {
        var clone = new EmevdEvent { Id = Id, RestBehavior = RestBehavior };
        clone.Instructions.AddRange(Instructions.Select(i => i.Clone()));
        clone.EventParameters.AddRange(EventParameters);
        return clone;
    }

    /// <summary>
    /// Sets the real argument format of every <c>init_event</c> from the started event's parameters
    /// (legacy <c>parse_event_init_args</c>).
    /// </summary>
    internal void ResolveInitEventArguments(IReadOnlyDictionary<uint, string> eventParameterTypes, InstructionCatalog catalog)
    {
        foreach (var instruction in Instructions.Where(i => i.IsInitEvent))
        {
            var defaults = StructFormat.Unpack(catalog.Format(instruction.Class, instruction.Index), instruction.ArgumentBytes);
            var target = Convert.ToUInt32(defaults[1], System.Globalization.CultureInfo.InvariantCulture);
            var format = eventParameterTypes.TryGetValue(target, out var types)
                ? "@iI" + (types.Length == 0 ? "I" : types)
                : "@iII";

            // Legacy fix: the parameter types only list the offsets the started event reads, so an event that
            // reads offset 4 but not 0 lost 4 bytes on every read/write in v0.4.x. Keep remaining bytes as 'I'.
            while (StructFormat.CalcSize(format) + 4 <= instruction.ArgumentBytes.Length)
            {
                format += "I";
            }

            instruction.ArgumentFormat = format;
            instruction.Args = [.. StructFormat.Unpack(instruction.ArgumentFormat, instruction.ArgumentBytes)];
        }
    }
}
