using EnemyRandomizer.Core.EnemyConfigs;

namespace EnemyRandomizer.Core.Tests.EnemyConfigs;

public class EnemyConfigFileTests
{
    private static readonly string[] Catalog =
    [
        "ID\tName\tType\tIsIgnored\tSize\tDifficulty\tLocations\tValidAI\tParam\tValidIdleAnimIDs\tComments",
        "c2500\tHollow\t0\t0\t1\t0\tm10;\t1\t2\t-1",
        "c4100\tArtorias\t1\t2\t3\t6\tm12;\t3\t4\t-1\tRemastered only",
        "c5201\tArm\t0\t1\t1\t5\tm14;\t5\t6\t-1\tbroken textures",
    ];

    [Theory]
    [InlineData(GameEdition.Ptde, false)]
    [InlineData(GameEdition.Remastered, true)]
    public void Flag_2_is_enabled_only_on_remastered(GameEdition edition, bool expected) =>
        Assert.Equal(expected, EnemyConfigFile.Parse(Catalog, edition)[1].Enabled);

    [Fact]
    public void Parse_reads_rows_and_comments()
    {
        var entries = EnemyConfigFile.Parse(Catalog, GameEdition.Ptde);

        Assert.Equal(3, entries.Count);
        Assert.Equal(new EnemyConfigEntry("c2500", "Hollow", true, ""), entries[0]);
        Assert.Equal(new EnemyConfigEntry("c5201", "Arm", false, "broken textures"), entries[2]);
    }

    [Fact]
    public void Write_matches_legacy_output()
    {
        var text = EnemyConfigFile.Write(Catalog, [false, true, true]);

        // Same as randomizer.py EnemyConfigSaveConfig: header, '\n' separated rows, column 4 = 0/1, trailing tab + comment.
        var expected = string.Join('\n',
            Catalog[0],
            "c2500\tHollow\t0\t1\t1\t0\tm10;\t1\t2\t-1\t",
            "c4100\tArtorias\t1\t0\t3\t6\tm12;\t3\t4\t-1\tRemastered only",
            "c5201\tArm\t0\t0\t1\t5\tm14;\t5\t6\t-1\tbroken textures");
        Assert.Equal(expected, text);
    }

    [Fact]
    public void Write_requires_one_flag_per_row() =>
        Assert.Throws<ArgumentException>(() => EnemyConfigFile.Write(Catalog, [true]));

    [Fact]
    public void Parse_rejects_short_rows() =>
        Assert.Throws<FormatException>(() => EnemyConfigFile.Parse([Catalog[0], "c1\tx\t0"], GameEdition.Ptde));
}
