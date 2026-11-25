using System.Globalization;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Formats.Emevd;

namespace EnemyRandomizer.Infrastructure.Randomization;

/// <summary>
/// Event changes for one map (legacy <c>EventTools</c>, <c>event_tools.py</c>): adds the randomizer's template events
/// (stay dead, tail cut, item lot on death) and their <c>init_event</c> calls to the map's constructor event,
/// replaces fixed events and patches single instruction arguments. Templates are cloned per map.
/// </summary>
public sealed class MapEventEditor
{
    private const int ConstructorEvent = 0;

    private readonly EmevdFile _file;
    private readonly IReadOnlyDictionary<uint, EmevdEvent> _templates;
    private readonly uint _disableRespawnEventId;
    private readonly uint _tailCutEventId;
    private readonly uint _itemLotEventId;
    private int _disableRespawnSlot;
    private int _tailCutSlot;
    private int _itemLotSlot;

    public MapEventEditor(
        EmevdFile file,
        string mapId,
        uint disableRespawnEventId,
        IReadOnlyDictionary<uint, EmevdEvent> templates,
        MapPatchSettings patches)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(patches);
        _file = file;
        _templates = templates;
        var t = patches.EventTemplates;
        _disableRespawnEventId = disableRespawnEventId;
        _tailCutEventId = (uint)(disableRespawnEventId + t.TailCutOffset);
        _itemLotEventId = (uint)(disableRespawnEventId + t.ItemLotOffset);

        AddTemplate(t.DisableRespawn, _disableRespawnEventId);
        AddTemplate(t.TailCut, _tailCutEventId);
        AddTemplate(t.ItemLotOnDeath, _itemLotEventId);

        foreach (var patch in patches.InstructionPatches.Where(p => p.Map == mapId))
        {
            Patch(patch);
        }

        if (patches.AlwaysReplacedEvents.TryGetValue(mapId, out var always))
        {
            ReplaceEvents(always);
        }
    }

    public EmevdFile File => _file;

    /// <summary>Keeps a roaming boss dead once killed.</summary>
    public void AddDisableRespawn(int entityId) =>
        Init("@iII", _disableRespawnSlot++, _disableRespawnEventId, entityId);

    /// <summary>Tail cut: hitting the tail part kills it and awards the tail item.</summary>
    public void AddTailCut(int enemyEntityId, int tailEntityId, TailCutDefinition tail)
    {
        ArgumentNullException.ThrowIfNull(tail);
        Init("@iIIIIII", _tailCutSlot++, _tailCutEventId, enemyEntityId, tail.PartNpcType, tail.PartNpcType, tailEntityId, tail.ItemLotId);
    }

    /// <summary>Awards an item lot when the enemy dies (keys, titanite the original enemy dropped).</summary>
    public void AddItemLotOnDeath(int entityId, int itemLot) =>
        Init("@iIII", _itemLotSlot++, _itemLotEventId, entityId, itemLot);

    /// <summary>Sets the enemy's team (e.g. hostile Lautrec / Andre).</summary>
    public void SetAllegiance(int entityId, int team) =>
        _file.Events[ConstructorEvent].Instructions.Add(EmevdInstruction.Create(2004, 2, "@iB", entityId, team));

    /// <summary>Replaces events with the same id by a copy of their template.</summary>
    public void ReplaceEvents(IEnumerable<uint> eventIds)
    {
        foreach (var id in eventIds)
        {
            if (!_templates.TryGetValue(id, out var template))
            {
                throw new InvalidDataException($"Event template {id} not found.");
            }

            for (var i = 0; i < _file.Events.Count; i++)
            {
                if (_file.Events[i].Id == id)
                {
                    _file.Events[i] = template.Clone();
                }
            }
        }
    }

    private void Init(string format, params object[] args) =>
        _file.Events[ConstructorEvent].Instructions.Add(EmevdInstruction.Create(EmevdInstruction.InitEventClass, 0, format, args));

    private void AddTemplate(uint templateId, uint newId)
    {
        if (!_templates.TryGetValue(templateId, out var template))
        {
            throw new InvalidDataException($"Event template {templateId} not found.");
        }

        var copy = template.Clone();
        copy.Id = newId;
        _file.Events.Add(copy);
    }

    private void Patch(InstructionPatch patch)
    {
        foreach (var ev in _file.Events.Where(e => e.Id == patch.EventId))
        {
            if (patch.InstructionIndex < ev.Instructions.Count
                && ev.Instructions[patch.InstructionIndex] is { } instruction
                && instruction.Class == patch.Class && instruction.Index == patch.Index
                && patch.ArgumentIndex < instruction.Args.Count)
            {
                instruction.Args[patch.ArgumentIndex] = Convert.ChangeType(patch.Value, instruction.Args[patch.ArgumentIndex].GetType(), CultureInfo.InvariantCulture);
            }
        }
    }
}
