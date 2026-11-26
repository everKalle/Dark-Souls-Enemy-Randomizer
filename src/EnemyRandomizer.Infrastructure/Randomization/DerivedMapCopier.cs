using EnemyRandomizer.Core.Randomization;
using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Data;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Formats.Msb;
using EnemyRandomizer.Infrastructure.Game;

namespace EnemyRandomizer.Infrastructure.Randomization;

/// <summary>Copies the randomized creatures into maps derived from them (Darkroot Garden #2).</summary>
public sealed class DerivedMapCopier(IGamePaths paths, GameFileLayout layout)
{
    /// <summary>
    /// Maps derived from a randomized one (Darkroot Garden #2) get the same creatures (legacy <c>copyDarkrootGarden</c>):
    /// matching rows take model, AI, param, entity id, animation and position; extra rows are appended.
    /// </summary>
    public void Copy(GameEdition edition, ReplacementTargets targets, FileTransaction transaction)
    {
        foreach (var map in layout.Maps.Where(m => m.CopyOf is not null))
        {
            var sourceMap = layout.Maps.Single(m => m.Id == map.CopyOf);
            var modelOffset = map.StartIndices[edition] - sourceMap.StartIndices[edition];
            var target = MsbFile.Read(File.ReadAllBytes(Path.Combine(paths.MapStudioCopyDirectory, map.Id + ".msb")));
            var source = MsbFile.Read(File.ReadAllBytes(paths.InGame(layout.MapStudio(sourceMap.Id))));
            var existing = target.Creatures.Rows.Count;

            for (var i = 0; i < source.Creatures.Rows.Count; i++)
            {
                var from = source.Creatures.Rows[i];
                if (i >= existing)
                {
                    // Fix: v0.4.x appended these rows (tail parts) without the model offset of the derived map.
                    var added = from.Clone();
                    added["Model"] = from.GetInt("Model") + modelOffset;
                    target.AddCreature(added);
                    continue;
                }

                var to = target.Creatures.Rows[i];
                if (targets.Find(to.Name[..Math.Min(5, to.Name.Length)]) is null)
                {
                    continue;
                }

                to["Model"] = from.GetInt("Model") + modelOffset;
                foreach (var field in new[] { "AI", "NPCParam", "EventEntityID", "AnimID", "X pos", "Y pos", "Z pos", "Rot X", "Rot Y", "Rot Z" })
                {
                    to[field] = from[field];
                }
            }

            for (var i = existing; i < source.Creatures.Rows.Count; i++)
            {
                target.Creatures.Rows[i]["PartIndex"] = source.Creatures.Rows[i]["PartIndex"];
            }

            transaction.WriteWithBackup(paths.InGame(layout.MapStudio(map.Id)), target.Write());
        }
    }
}
