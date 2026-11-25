namespace EnemyRandomizer.Core.Configuration;

/// <summary>
/// Fixed per-map adjustments applied while randomizing (<c>config/map-patches.json</c>, section <c>MapPatches</c>).
/// Legacy <c>randomizer_rng.py:77-181</c> and <c>event_tools.py:36-45</c>.
/// </summary>
public sealed class MapPatchSettings : IValidatableSettings
{
    public static string SectionName => "MapPatches";

    /// <summary>Map id → part name → new position (x, y, z) for enemies that ambush from unreachable spots.</summary>
    public Dictionary<string, Dictionary<string, float[]>> AmbushPositions { get; set; } = [];

    /// <summary>Map id → part name → new position (x, y, z), applied always.</summary>
    public Dictionary<string, Dictionary<string, float[]>> OtherPositions { get; set; } = [];

    /// <summary>Map id → part name → item lot given when the replacement is killed.</summary>
    public Dictionary<string, Dictionary<string, int>> ItemLotsToAward { get; set; } = [];

    /// <summary>Model id → new team/allegiance value.</summary>
    public Dictionary<string, int> CharacterAllegiances { get; set; } = [];

    public List<TailCutDefinition> TailCuts { get; set; } = [];

    public EasyAsylumSettings EasyAsylum { get; set; } = new();

    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();
        foreach (var (mapId, parts) in AmbushPositions.Concat(OtherPositions))
        {
            errors.AddRange(parts.Where(p => p.Value.Length != 3)
                                 .Select(p => $"Position of '{mapId}/{p.Key}' needs exactly 3 values (x, y, z)."));
        }

        errors.AddRange(ValidationHelpers.Duplicates(TailCuts.Select(t => t.ModelId), "tail cut model"));
        errors.AddRange(EasyAsylum.Validate());
        return errors;
    }
}

/// <summary>Bosses with a cuttable tail: extra model, NpcParam and tail-cut event values.</summary>
public sealed class TailCutDefinition
{
    public string ModelId { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>Index of the tail model relative to the map start index.</summary>
    public int RelativeModelIndex { get; set; }

    public int NpcParamId { get; set; }

    /// <summary>NPC type of the part used by the tail-cut event.</summary>
    public int PartNpcType { get; set; }

    /// <summary>Item lot awarded when the tail is cut.</summary>
    public int ItemLotId { get; set; }
}

/// <summary>Fixed pools used for the Northern Undead Asylum in "easy asylum" modes.</summary>
public sealed class EasyAsylumSettings
{
    /// <summary>Indices into the enemy catalog (valid_new.txt rows) allowed for normal enemies.</summary>
    public List<int> NormalEnemyIndices { get; set; } = [];

    /// <summary>Indices into the enemy catalog allowed for bosses.</summary>
    public List<int> BossIndices { get; set; } = [];

    /// <summary>Asylum parts that use the easy pools.</summary>
    public List<string> Targets { get; set; } = [];

    internal IEnumerable<string> Validate()
    {
        if (NormalEnemyIndices.Count == 0 || BossIndices.Count == 0 || Targets.Count == 0)
        {
            yield return "MapPatches.EasyAsylum needs normal indices, boss indices and targets.";
        }
    }
}
