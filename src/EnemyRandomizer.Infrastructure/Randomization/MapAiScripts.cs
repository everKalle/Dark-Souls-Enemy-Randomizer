using EnemyRandomizer.Core.Data;
using EnemyRandomizer.Formats.Binary;
using EnemyRandomizer.Formats.Lua;

namespace EnemyRandomizer.Infrastructure.Randomization;

/// <summary>
/// The AI script archive of one map while it is randomized: adds the scripts, global functions and info entries a
/// new enemy AI needs (legacy <c>randomizer_rng.py:1625-1631</c>, <c>BndData.addAuto</c>).
/// </summary>
public sealed class MapAiScripts
{
    private readonly LuaBnd _archive;
    private readonly LuaGnl _gnl;
    private readonly LuaInfo _info;
    private readonly string _scriptDirectory;

    public MapAiScripts(byte[] archive, string scriptDirectory)
    {
        _archive = LuaBnd.Read(archive);
        _gnl = LuaGnl.Read(_archive.GnlData);
        _info = LuaInfo.Read(_archive.InfoData);
        _scriptDirectory = scriptDirectory;
    }

    /// <summary>Adds what <paramref name="ai"/> needs, unless the map already knows that AI.</summary>
    public void Require(AiReference ai)
    {
        ArgumentNullException.ThrowIfNull(ai);
        var values = ai.InfoEntries.Select(e => new LuaInfoValue(
            (uint)e.NpcAi, GameText.Encode(e.Primary), GameText.Encode(e.Secondary),
            (byte)e.FlagA, (byte)e.FlagB, (byte)e.FlagC, (byte)e.FlagD)).ToList();

        if (_info.AddEntryAuto(values))
        {
            return;
        }

        _gnl.AddEntriesAuto(ai.GnlFunctions.Select(GameText.Encode));
        foreach (var script in new[] { ai.BattleScript, ai.LogicScript }.Where(s => s.Length > 0))
        {
            var file = Path.Combine(_scriptDirectory, script);
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"AI script '{script}' for AI {ai.NpcAi} not found in the extracted scripts.", file);
            }

            _archive.Add(script, File.ReadAllBytes(file));
        }
    }

    public byte[] Write() => _archive.Write(_gnl.Write(), _info.Write());
}
