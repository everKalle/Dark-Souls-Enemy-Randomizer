using EnemyRandomizer.Core.Data;
using EnemyRandomizer.Core.Randomization;
using EnemyRandomizer.Formats.Msb;

namespace EnemyRandomizer.Infrastructure.Randomization;

/// <summary>
/// Applies a <see cref="MapPlan"/> to one map (legacy <c>randomizer_rng.py:1594-1752</c>): new model, AI, param and
/// idle animation for each creature row, position fixes, tail parts, event ids and event calls, layout reference
/// rows and log lines.
/// </summary>
public sealed class MapPlanApplier(
    MsbFile msb,
    MapAiScripts aiScripts,
    MapEventEditor events,
    IReadOnlyDictionary<int, AiReference> aiReferences,
    int modelStartIndex,
    int firstEventEntityId)
{
    private const int NoEntity = -1;
    private const int TailAi = 1;

    /// <summary>Next free event entity id (written as <c>levID</c> to the layout reference).</summary>
    public int NextEventEntityId { get; private set; } = firstEventEntityId;

    public void Apply(MapPlan plan, LayoutReferenceWriter reference, RunLog log)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(log);

        var creatures = msb.Creatures.Rows;
        if (plan.Decisions.Count > creatures.Count)
        {
            throw new InvalidDataException(
                $"{plan.MapId}: the reference file lists {plan.Decisions.Count} creatures, the map has {creatures.Count}.");
        }

        foreach (var decision in plan.Decisions)
        {
            if (decision.Outcome == ReplacementOutcome.Replaced)
            {
                var row = creatures[decision.Row];
                Replace(row, decision);
                reference.Row(row);
                log.Line($"Replacing ({decision.PartName}) {decision.OriginalName} with ({decision.Enemy!.Id}) {decision.Enemy.Name}" +
                         $"[{decision.Enemy.Index}] ai = {(decision.ChangeAi ? decision.Ai : "<original>")}; param = {decision.Param}");
            }
            else if (decision.Outcome is not ReplacementOutcome.NotATarget and not ReplacementOutcome.SpecialCase)
            {
                log.Line($"Did not replace ({decision.PartName}) {decision.OriginalName} - {Reason(decision.Outcome)}");
            }
        }
    }

    private void Replace(MsbRow row, CreatureDecision decision)
    {
        var enemy = decision.Enemy!;
        row["NPCParam"] = decision.Param;
        if (decision.ChangeAi)
        {
            row["AI"] = decision.Ai;
        }

        row["Model"] = modelStartIndex + enemy.Index;

        if (aiReferences.TryGetValue(decision.Ai, out var ai))
        {
            aiScripts.Require(ai);
        }
        else
        {
            throw new InvalidDataException($"AI {decision.Ai} of {enemy.Id} is not listed in airef.csv.");
        }

        if (decision.IdleAnimation is { } animation)
        {
            row["AnimID"] = animation;
        }

        if (decision.Position is { } position)
        {
            (row["X pos"], row["Y pos"], row["Z pos"]) = (position[0], position[1], position[2]);
        }

        if (decision.Rotation is { } rotation)
        {
            (row["Rot X"], row["Rot Y"], row["Rot Z"]) = (rotation[0], rotation[1], rotation[2]);
        }

        if (decision.ItemLot is { } itemLot)
        {
            events.AddItemLotOnDeath(EntityId(row), itemLot);
        }

        if (decision.DisableRespawn)
        {
            events.AddDisableRespawn(EntityId(row));
        }

        if (decision.TailCut is { } tail)
        {
            var tailRow = row.Clone();
            tailRow.Name = row.Name[..Math.Min(6, row.Name.Length)] + "tail";
            tailRow["Model"] = modelStartIndex + tail.RelativeModelIndex;
            tailRow["EventEntityID"] = NextEventEntityId++;
            tailRow["NPCParam"] = tail.NpcParamId;
            tailRow["AI"] = TailAi;
            msb.AddCreature(tailRow);
            events.AddTailCut(EntityId(row), tailRow.GetInt("EventEntityID"), tail);
        }

        if (decision.Allegiance is { } team)
        {
            events.SetAllegiance(EntityId(row), team);
        }

        if (decision.EventFixes.Count > 0)
        {
            events.ReplaceEvents(decision.EventFixes);
        }
    }

    /// <summary>The row's event entity id; assigns the next free one when it has none.</summary>
    private int EntityId(MsbRow row)
    {
        if (row.GetInt("EventEntityID") == NoEntity)
        {
            row["EventEntityID"] = NextEventEntityId++;
        }

        return row.GetInt("EventEntityID");
    }

    private static string Reason(ReplacementOutcome outcome) => outcome switch
    {
        ReplacementOutcome.RandomChance => "Random chance",
        ReplacementOutcome.MimicKept => "Mimic replacement disabled",
        ReplacementOutcome.NoFittingDifficulty => "Failed to find fitting enemy with appropriate difficulty",
        ReplacementOutcome.ModeOff => "Replacement mode is off",
        _ => "No valid replacement found",
    };
}
