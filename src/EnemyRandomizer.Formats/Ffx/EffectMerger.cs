using System.Globalization;
using System.Text.RegularExpressions;
using EnemyRandomizer.Formats.Bnd;

namespace EnemyRandomizer.Formats.Ffx;

/// <summary>
/// Collects the effects of all map effect bundles into <c>FRPG_SfxBnd_CommonEffects.ffxbnd</c>, so an enemy shows
/// its effects in every map (legacy <c>FFXData.AddEverythingToCommon</c>). Entry ids: &lt; 100000 = .ffx,
/// &lt; 200000 = .tpf, otherwise models. On Remastered only enemy effects with id &lt; 20001 are taken.
/// </summary>
public static partial class EffectMerger
{
    private const int TpfIdStart = 100000;
    private const int ModelIdStart = 200000;
    private const int RemasteredMaxEnemyId = 20001;

    /// <summary>
    /// Returns the new CommonEffects entries: existing ffx, then tpf, then model entries, each group followed by the
    /// new entries (renumbered after the group's highest id). <c>Changed</c> is false when nothing was added.
    /// </summary>
    public static (List<BndEntry> Entries, bool Changed) Merge(
        IEnumerable<IReadOnlyList<BndEntry>> mapBundles,
        IReadOnlyList<BndEntry> common,
        bool remastered)
    {
        ArgumentNullException.ThrowIfNull(mapBundles);
        ArgumentNullException.ThrowIfNull(common);

        var collected = new List<BndEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in mapBundles.SelectMany(b => b))
        {
            if (!seen.Contains(entry.Name) && (!remastered || IsWantedOnRemastered(entry.Name)))
            {
                seen.Add(entry.Name);
                collected.Add(entry);
            }
        }

        var existing = common.Select(e => e.Name).ToHashSet(StringComparer.Ordinal);
        var ffx = common.Where(e => e.Id < TpfIdStart).ToList();
        var tpf = common.Where(e => e.Id is >= TpfIdStart and < ModelIdStart).ToList();
        var models = common.Where(e => e.Id >= ModelIdStart).ToList();

        var nextFfx = ffx.Select(e => e.Id).DefaultIfEmpty(0).Max() + 1;
        var nextTpf = tpf.Select(e => e.Id).DefaultIfEmpty(0).Max() + 1;
        var nextModel = models.Select(e => e.Id).DefaultIfEmpty(0).Max() + 1;

        foreach (var entry in collected.Where(e => !existing.Contains(e.Name)))
        {
            if (entry.Id < TpfIdStart)
            {
                ffx.Add(entry with { Id = nextFfx++ });
            }
            else if (entry.Id < ModelIdStart)
            {
                tpf.Add(entry with { Id = nextTpf++ });
            }
            else
            {
                models.Add(entry with { Id = nextModel++ });
            }
        }

        var merged = ffx.Concat(tpf).Concat(models).ToList();
        return (merged, merged.Count != common.Count);
    }

    /// <summary>PTDE: after merging, map bundles keep only their first entry (prevents the effect memory crash).</summary>
    public static List<BndEntry> TruncateMapBundle(IReadOnlyList<BndEntry> entries) => [.. entries.Take(1)];

    private static bool IsWantedOnRemastered(string name)
    {
        foreach (var pattern in new[] { EnemyEffect(), EnemyModel(), EnemyTexture() })
        {
            var match = pattern.Match(name);
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) < RemasteredMaxEnemyId;
            }
        }

        return false;
    }

    // Same (unescaped '.') patterns as the legacy code, anchored at the start like Python's re.match.
    [GeneratedRegex(@"^.*00([1-9][0-9][0-9][0-9][0-9]).ffx")]
    private static partial Regex EnemyEffect();

    [GeneratedRegex(@"^.*s([1-9][0-9][0-9][0-9][0-9]).flver")]
    private static partial Regex EnemyModel();

    [GeneratedRegex(@"^.*s([1-9][0-9][0-9][0-9][0-9]).tpf")]
    private static partial Regex EnemyTexture();
}
