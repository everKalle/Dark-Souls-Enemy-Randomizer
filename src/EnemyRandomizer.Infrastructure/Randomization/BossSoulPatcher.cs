using EnemyRandomizer.Core.Randomization;
using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Data;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Formats.Param;
using EnemyRandomizer.Infrastructure.Game;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Randomization;

/// <summary>Adds the "with souls" boss variants to NpcParam inside GameParam (legacy <c>applyBossSouls</c>).</summary>
public sealed class BossSoulPatcher(
    IGamePaths paths, GameFileLayout layout, IOptions<GameDataSettings> gameData, IOptions<ReplacementRuleSettings> rules)
{
    /// <summary>"With souls" boss copies for bosses on normal enemy spots (legacy <c>applyBossSouls</c>).</summary>
    public void Apply(GameEdition edition, RunOptions options, FileTransaction transaction)
    {
        var file = paths.InGame(layout.GameParam(edition));
        var archive = GameArchive.Read(file);

        // The patch always starts from the original NpcParam kept by the first-time setup, never from the current
        // (possibly already patched) one, so repeated runs cannot stack variants.
        var originalFile = Path.Combine(paths.ParamBackupDirectory, Path.GetFileName(file));
        var original = File.Exists(originalFile) ? GameArchive.Read(originalFile) : archive;
        var index = gameData.Value.NpcParamIndex;
        var bosses = rules.Value.BossSouls.Select(b => new BossSoul(b.NpcParamId, b.Souls, b.Name)).ToList();

        var npcParam = NpcParam.Read(original.Entries[index].Data);
        npcParam.AddSoulVariants(bosses);
        npcParam.ApplyBossSoulCount(bosses, options.BossSoulDrops);
        npcParam.SetRespawnFlags(bosses, disableRespawn: false, disableInitializeDead: false);
        npcParam.RemoveItemLots(rules.Value.NpcParamsWithoutItemLot);

        archive.Entries[index] = archive.Entries[index] with { Data = npcParam.Write() };
        transaction.Write(file, archive.ToBytes());
    }
}
