using System.Text;
using EnemyRandomizer.Formats.Bnd;
using EnemyRandomizer.Formats.Ffx;
using EnemyRandomizer.Formats.Param;

namespace EnemyRandomizer.Formats.Tests;

public class NpcParamTests
{
    private static readonly BossSoul[] Bosses = [new(223000, 20000, "Stray Demon"), new(225000, 3000, "Taurus Demon")];

    /// <summary>NpcParam with rows 100, 223000, 225000, 278000; vowType of row 0 = 1 (asymmetric bits).</summary>
    private static NpcParam Sample()
    {
        var schema = NpcParam.StandardSchema;
        var file = new ParamFile { DataSize = schema.RowSize, Unknown1 = 7, Unknown2 = 3 };
        Encoding.ASCII.GetBytes("NPC_PARAM_ST").CopyTo(file.TypeName, 0);
        foreach (var id in new[] { 100, 223000, 225000, 278000 })
        {
            var data = new byte[schema.RowSize];
            BitConverter.GetBytes(id).CopyTo(data, 0);         // behaviorVariationId = id, to tell rows apart
            file.Rows.Add(new ParamRow(id, "row " + id, data));
        }

        var param = NpcParam.Read(file.Write());
        param.Records[0].Bits["vowType:3"] = 1;
        return NpcParam.Read(param.Write());
    }

    [Fact]
    public void Schema_matches_legacy_row_size()
    {
        var schema = NpcParam.StandardSchema;
        Assert.Equal(122, schema.Fields.Count);
        Assert.Equal(11, schema.BitBytes);
    }

    [Fact]
    public void Read_then_write_is_byte_identical()
    {
        var bytes = Sample().Write();
        Assert.Equal(bytes, NpcParam.Read(bytes).Write());
    }

    [Fact]
    public void Multi_bit_fields_keep_their_value()
    {
        // v0.4.x read "vowType" MSB-first and wrote it LSB-first (1 became 4 after one save).
        var reread = NpcParam.Read(Sample().Write());
        Assert.Equal(1UL, reread.Records[0].Bits["vowType:3"]);
    }

    [Fact]
    public void Soul_variants_are_added_once_with_souls_and_flags()
    {
        var param = Sample();

        Assert.True(param.AddSoulVariants(Bosses));
        Assert.False(param.AddSoulVariants(Bosses));
        param.ApplyBossSoulCount(Bosses, 50);
        param.SetRespawnFlags(Bosses, disableRespawn: true, disableInitializeDead: false);
        var reread = NpcParam.Read(param.Write());

        Assert.Equal([100, 223000, 225000, 278000, 223050, 225050], reread.File.Rows.Select(r => r.Id));
        Assert.Equal("Stray Demon with souls", reread.File.Rows[4].Name);
        Assert.Equal(10000u, reread.Records[4].Values["getSoul"]);
        Assert.Equal(1500u, reread.Records[5].Values["getSoul"]);
        Assert.True(reread.Records[4].Flag("disableRespawn:1"));
        Assert.Equal(0u, reread.Records[1].Values["getSoul"]);              // original boss row untouched
        Assert.Equal(223000, reread.Records[4].Values["behaviorVariationId"]); // copied from the boss row
    }

    [Fact]
    public void Item_lots_are_removed()
    {
        var param = Sample();
        param.RemoveItemLots([278000]);
        Assert.Equal(-1, NpcParam.Read(param.Write()).Records[3].Values["itemLotId_1"]);
    }
}

public class EffectMergerTests
{
    private static BndEntry E(int id, string name) => new(id, name, [(byte)id]);

    [Fact]
    public void New_effects_are_appended_per_group_with_new_ids()
    {
        List<BndEntry> common = [E(5, "f0001000.ffx"), E(100002, "s10010.tpf"), E(200001, "s10010.flver")];
        IReadOnlyList<BndEntry>[] maps =
        [
            [E(1, "f0001000.ffx"), E(2, "f0012345.ffx"), E(100001, "s12345.tpf")],
            [E(3, "f0012345.ffx"), E(200009, "s12345.flver")],
        ];

        var (entries, changed) = EffectMerger.Merge(maps, common, remastered: false);

        Assert.True(changed);
        Assert.Equal(
            [(5, "f0001000.ffx"), (6, "f0012345.ffx"), (100002, "s10010.tpf"), (100003, "s12345.tpf"), (200001, "s10010.flver"), (200002, "s12345.flver")],
            entries.Select(e => (e.Id, e.Name)));
    }

    [Fact]
    public void Remastered_takes_only_enemy_effects_below_20001()
    {
        IReadOnlyList<BndEntry>[] maps = [[E(1, "f0012345.ffx"), E(2, "f0025000.ffx"), E(3, "other.ffx"), E(100001, "s19999.tpf")]];

        var (entries, _) = EffectMerger.Merge(maps, [], remastered: true);

        Assert.Equal(["f0012345.ffx", "s19999.tpf"], entries.Select(e => e.Name));
    }

    [Fact]
    public void Nothing_new_means_unchanged()
    {
        List<BndEntry> common = [E(1, "a.ffx")];
        var (_, changed) = EffectMerger.Merge([[E(9, "a.ffx")]], common, remastered: false);
        Assert.False(changed);
    }

    [Fact]
    public void Map_bundles_are_truncated_to_the_first_entry() =>
        Assert.Single(EffectMerger.TruncateMapBundle([E(1, "a"), E(2, "b")]));
}
