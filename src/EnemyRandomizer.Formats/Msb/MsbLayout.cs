using System.Text.Json;

namespace EnemyRandomizer.Formats.Msb;

/// <summary>
/// Field layout of one MSB row type (legacy <c>msbdata.py</c>, embedded <c>Resources/msb-layouts.json</c>).
/// Numeric fields before the name form <see cref="BeforeName"/>, those after the name (and sib path) form
/// <see cref="AfterName"/>; both are native-aligned <c>struct</c> formats.
/// </summary>
public sealed class MsbLayout
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Definitions = new(LoadDefinitions);
    private static readonly Dictionary<string, MsbLayout> Cache = [];
    private static readonly Lock CacheLock = new();

    private readonly List<string> _fields = [];

    private MsbLayout(string name)
    {
        Name = name;
        Build(name);
    }

    public string Name { get; }

    public IReadOnlyList<string> Fields => _fields;

    public string BeforeName { get; private set; } = "@";

    public string AfterName { get; private set; } = "@";

    public int NameIndex { get; private set; } = -1;

    public int PartIndex1 { get; private set; } = -1;

    public int PartIndex2 { get; private set; } = -1;

    public int PartIndex3 { get; private set; } = -1;

    public int PointIndex1 { get; private set; } = -1;

    public int PointIndex2 { get; private set; } = -1;

    /// <summary>True when the name is followed by a sib path string.</summary>
    public bool HasSib => NameIndex + 1 < _fields.Count && _fields[NameIndex + 1] == "Sibpath";

    /// <summary>Index of the first field after the name (and sib path).</summary>
    public int AfterNameIndex => NameIndex + (HasSib ? 2 : 1);

    public static MsbLayout Get(string name)
    {
        lock (CacheLock)
        {
            if (!Cache.TryGetValue(name, out var layout))
            {
                layout = new MsbLayout(name);
                Cache[name] = layout;
            }

            return layout;
        }
    }

    /// <summary>Index of a field by name (first match), -1 when missing.</summary>
    public int IndexOf(string field) => _fields.IndexOf(field);

    private void Build(string name)
    {
        if (!Definitions.Value.TryGetValue(name, out var definition))
        {
            throw new InvalidDataException($"Invalid MSB layout name: {name}");
        }

        foreach (var part in definition.Split('|'))
        {
            var pieces = part.Split(':');
            if (pieces.Length == 1)
            {
                if (pieces[0] != "[EVENT]")
                {
                    throw new InvalidDataException($"Invalid single tag: {pieces[0]}");
                }

                Build("eventTemplate");
                continue;
            }

            if (pieces.Length == 3)
            {
                var index = _fields.Count;
                switch (pieces[2])
                {
                    case "[NAME]": NameIndex = index; break;
                    case "[PRT1]": PartIndex1 = index; break;
                    case "[PRT2]": PartIndex2 = index; break;
                    case "[PRT3]": PartIndex3 = index; break;
                    case "[PNT1]": PointIndex1 = index; break;
                    case "[PNT2]": PointIndex2 = index; break;
                    default: throw new InvalidDataException($"Invalid index tag: {pieces[2]}");
                }
            }

            Add(pieces[0], pieces[1]);
        }
    }

    private void Add(string field, string type)
    {
        if (type.Length == 1)
        {
            _fields.Add(field);
            if (NameIndex == -1)
            {
                BeforeName += type;
            }
            else
            {
                AfterName += type;
            }
        }
        else if (type == "string")
        {
            _fields.Add(field);
        }
        else
        {
            throw new InvalidDataException($"Invalid field type: '{type}'");
        }
    }

    private static Dictionary<string, string> LoadDefinitions()
    {
        using var stream = typeof(MsbLayout).Assembly.GetManifestResourceStream("EnemyRandomizer.Formats.Resources.msb-layouts.json")
                           ?? throw new InvalidOperationException("Embedded msb-layouts.json not found.");
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.GetProperty("Layouts").EnumerateObject()
                       .ToDictionary(p => p.Name, p => p.Value.GetString() ?? "");
    }
}
