using System.Globalization;
using System.Text.Json;

namespace EnemyRandomizer.Formats.Emevd;

/// <summary>
/// Names and argument formats of all EMEVD instructions (embedded <c>Resources/emevd-instructions.json</c>,
/// converted 1:1 from legacy <c>method_names.py</c>: <c>METHOD_NAMES</c> + <c>IAT</c>).
/// </summary>
public sealed class InstructionCatalog
{
    private static readonly Lazy<InstructionCatalog> DefaultCatalog = new(LoadEmbedded);

    private readonly Dictionary<(int Class, int Index), string> _names;
    private readonly Dictionary<(int Class, int Index), string> _formats;
    private readonly Dictionary<string, (int Class, int Index)> _byName;

    public InstructionCatalog(
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> names,
        IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> formats)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(formats);

        _names = Flatten(names);
        _formats = Flatten(formats);
        _byName = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        foreach (var (key, name) in _names)
        {
            if (!_byName.TryAdd(name, key))
            {
                throw new InvalidDataException($"Duplicate method name '{name}'.");
            }
        }
    }

    /// <summary>The catalog shipped with the randomizer.</summary>
    public static InstructionCatalog Default => DefaultCatalog.Value;

    public int Count => _names.Count;

    public string Name(int instructionClass, int index) =>
        _names.TryGetValue((instructionClass, index), out var name)
            ? name
            : throw new KeyNotFoundException($"Method name [{instructionClass}, {index}] is undefined.");

    public string Format(int instructionClass, int index) =>
        _formats.TryGetValue((instructionClass, index), out var format)
            ? format
            : throw new KeyNotFoundException($"Argument format [{instructionClass}, {index}] is undefined.");

    public (int Class, int Index) Lookup(string name) =>
        _byName.TryGetValue(name, out var key)
            ? key
            : throw new KeyNotFoundException($"Unknown instruction '{name}'.");

    private static Dictionary<(int, int), string> Flatten(IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> source) =>
        source.SelectMany(c => c.Value.Select(i => (Key: (c.Key, i.Key), i.Value)))
              .ToDictionary(x => x.Key, x => x.Value);

    private static InstructionCatalog LoadEmbedded()
    {
        var assembly = typeof(InstructionCatalog).Assembly;
        using var stream = assembly.GetManifestResourceStream("EnemyRandomizer.Formats.Resources.emevd-instructions.json")
                           ?? throw new InvalidOperationException("Embedded emevd-instructions.json not found.");
        using var document = JsonDocument.Parse(stream);
        return new InstructionCatalog(Section(document, "MethodNames"), Section(document, "ArgumentFormats"));
    }

    private static Dictionary<int, IReadOnlyDictionary<int, string>> Section(JsonDocument document, string name) =>
        document.RootElement.GetProperty(name).EnumerateObject().ToDictionary(
            c => int.Parse(c.Name, CultureInfo.InvariantCulture),
            c => (IReadOnlyDictionary<int, string>)c.Value.EnumerateObject().ToDictionary(
                i => int.Parse(i.Name, CultureInfo.InvariantCulture),
                i => i.Value.GetString() ?? ""));
}
