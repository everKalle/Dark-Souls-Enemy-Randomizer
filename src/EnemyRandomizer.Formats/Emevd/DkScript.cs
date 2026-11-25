using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EnemyRandomizer.Formats.Binary;

namespace EnemyRandomizer.Formats.Emevd;

/// <summary>
/// Text form of EMEVD used for the randomizer's event templates (legacy <c>import_dkscript</c>/<c>export_dkscript</c>).
/// <code>
/// def 11810310, 0 (i: target, I: flag) {
///     int myVar = 5
///     SomeInstruction(target, myVar, 1.5)
///     init_event(0, 11810311, 0)
/// }
/// </code>
/// Event arguments are referenced by name; they become EMEVD parameters.
/// </summary>
public static partial class DkScript
{
    public static EmevdFile Compile(string text, InstructionCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        catalog ??= InstructionCatalog.Default;

        var file = new EmevdFile();
        var importArgs = new Dictionary<EmevdInstruction, string[]>();
        var argMap = new Dictionary<string, int>(StringComparer.Ordinal);
        var variables = new Dictionary<string, object>(StringComparer.Ordinal);
        EmevdEvent? current = null;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r'); // Python text mode normalizes line endings
            if (EventDefinition().Match(line) is { Success: true } def)
            {
                argMap.Clear();
                current = new EmevdEvent
                {
                    Id = uint.Parse(def.Groups[1].Value, CultureInfo.InvariantCulture),
                    RestBehavior = uint.Parse(def.Groups[2].Value, CultureInfo.InvariantCulture),
                };

                var types = "@";
                var pieces = def.Groups[3].Value.Split(',');
                for (var i = 0; i < pieces.Length; i++)
                {
                    if (EventArgument().Match(pieces[i]) is { Success: true } arg)
                    {
                        var type = arg.Groups[1].Value[0];
                        types += type;
                        var offset = StructFormat.CalcSize(types) - StructFormat.CalcSize("@" + type);
                        current.EventParameters.Add((offset, type));
                        argMap[arg.Groups[2].Value.Trim()] = i;
                    }
                }
            }
            else if (VariableDefinition().Match(line) is { Success: true } variable)
            {
                var value = variable.Groups[5].Value.Trim();
                variables[variable.Groups[3].Value] = variable.Groups[2].Value == "int"
                    ? ParseInt(value)
                    : ParseFloat(value);
            }
            else if (MethodCall().Match(line) is { Success: true } call)
            {
                var (cls, index) = catalog.Lookup(call.Groups[2].Value);
                var instruction = new EmevdInstruction { Class = cls, Index = index, ArgumentFormat = catalog.Format(cls, index) };
                var pieces = call.Groups[4].Value.Split(',');

                if (instruction.IsInitEvent)
                {
                    importArgs[instruction] = pieces;
                }
                else
                {
                    AddArguments(instruction, pieces, current!, argMap, variables);
                }

                var sorted = instruction.Parameters.OrderBy(p => p.SourceStartByte).ToList();
                instruction.Parameters.Clear();
                instruction.Parameters.AddRange(sorted);
                current!.Instructions.Add(instruction);
            }
            else if (EventEnd().IsMatch(line) && current is not null)
            {
                file.Events.Add(current);
                current = null;
            }
        }

        ResolveInitEvents(file, importArgs);
        return file;
    }

    public static string Export(EmevdFile file, InstructionCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(file);
        catalog ??= InstructionCatalog.Default;

        var text = new StringBuilder();
        foreach (var ev in file.Events)
        {
            var parameters = string.Join(", ", ev.EventParameters.Select((p, i) => $"{p.Type}: arg{i}"));
            text.Append(CultureInfo.InvariantCulture, $"def {ev.Id}, {ev.RestBehavior} ({parameters}) {{\n");
            foreach (var instruction in ev.Instructions)
            {
                var args = instruction.Args.Select((value, i) =>
                {
                    var parameter = instruction.Parameters.LastOrDefault(p => instruction.ArgumentIndexAt(p.DestinationStartByte, p.Length) == i);
                    return parameter is null
                        ? FormatValue(value)
                        : "arg" + ev.EventParameters.FindIndex(e => e.Offset == parameter.SourceStartByte);
                });
                text.Append(CultureInfo.InvariantCulture, $"\t{catalog.Name(instruction.Class, instruction.Index)}({string.Join(", ", args)})\n");
            }

            text.Append("}\n\n");
        }

        return text.ToString();
    }

    private static void AddArguments(
        EmevdInstruction instruction,
        string[] pieces,
        EmevdEvent current,
        Dictionary<string, int> argMap,
        Dictionary<string, object> variables)
    {
        var codes = StructFormat.Codes(instruction.ArgumentFormat);
        for (var i = 0; i < pieces.Length; i++)
        {
            var value = pieces[i].Trim();
            if (value.Length == 0)
            {
                continue;
            }

            var type = codes[i];
            if (argMap.TryGetValue(value, out var eventArg))
            {
                instruction.Args.Add(type == 'f' ? (object)0f : 0L);
                var size = StructFormat.CalcSize("@" + type);
                instruction.Parameters.Add(new EmevdParameter
                {
                    DestinationStartByte = StructFormat.CalcSize("@" + codes[..(i + 1)]) - size,
                    SourceStartByte = current.EventParameters[eventArg].Offset,
                    Length = size,
                    ArgumentType = type,
                });
            }
            else if (variables.TryGetValue(value, out var variable))
            {
                instruction.Args.Add(variable);
            }
            else
            {
                try
                {
                    instruction.Args.Add(type == 'f' ? (object)ParseFloat(value) : ParseInt(value));
                }
                catch (FormatException)
                {
                    throw new FormatException("Undefined variable: " + value);
                }
            }
        }
    }

    /// <summary>Legacy <c>parse_event_init_args_import</c>: format depends on the started event's parameters.</summary>
    private static void ResolveInitEvents(EmevdFile file, Dictionary<EmevdInstruction, string[]> importArgs)
    {
        var types = file.Events.GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.Last().ParameterTypes);
        foreach (var (instruction, pieces) in importArgs)
        {
            var target = (uint)ParseInt(pieces[1].Trim());
            if (types.TryGetValue(target, out var parameterTypes))
            {
                if (parameterTypes.Length > 0)
                {
                    instruction.ArgumentFormat = "@iI" + parameterTypes;
                }
            }
            else
            {
                instruction.ArgumentFormat = "@iII";
            }

            // More values than the started event declares (exported from a file read losslessly): extra 4-byte ints.
            var values = pieces.Select(p => p.Trim()).ToList();
            while (StructFormat.Codes(instruction.ArgumentFormat).Length < values.Count)
            {
                var next = values[StructFormat.Codes(instruction.ArgumentFormat).Length];
                instruction.ArgumentFormat += next.StartsWith('-') ? "i" : "I";
            }

            var codes = StructFormat.Codes(instruction.ArgumentFormat);
            instruction.Args = [.. values.Select((value, i) => codes[i] == 'f' ? (object)ParseFloat(value) : ParseInt(value))];
        }
    }

    private static long ParseInt(string value) =>
        long.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

    private static float ParseFloat(string value) =>
        (float)double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>Formats like Python <c>str()</c>: ints as is, floats with at least one decimal (<c>1.0</c>).</summary>
    private static string FormatValue(object value)
    {
        if (value is not float f)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        var d = (double)f;
        var text = d.ToString("R", CultureInfo.InvariantCulture).Replace("E", "e", StringComparison.Ordinal);
        return text.Contains('.', StringComparison.Ordinal) || text.Contains('e', StringComparison.Ordinal)
               || text.Contains("Infinity", StringComparison.Ordinal) || text == "NaN"
            ? text
            : text + ".0";
    }

    [GeneratedRegex(@"^def ([0-9]+), *(0|1|2) *\((.*)\) *{")]
    private static partial Regex EventDefinition();

    [GeneratedRegex(@"^ *(b|B|i|I|f|h): *(.*) *")]
    private static partial Regex EventArgument();

    [GeneratedRegex(@"^( |\t)*(int|float) +(([A-Z]|[a-z]|\.|_|[0-9])+) * = *(.*)")]
    private static partial Regex VariableDefinition();

    // Same character set as the legacy compiler: instructions with non-ASCII names cannot be called from dkscript.
    [GeneratedRegex(@"^( |\t)*(([A-Z]|[a-z]|\.|_|[0-9])+)\((.*)\)")]
    private static partial Regex MethodCall();

    [GeneratedRegex(@"^ *} *")]
    private static partial Regex EventEnd();
}
