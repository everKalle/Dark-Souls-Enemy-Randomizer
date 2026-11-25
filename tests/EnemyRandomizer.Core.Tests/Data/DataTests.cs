using EnemyRandomizer.Core.Data;

namespace EnemyRandomizer.Core.Tests.Data;

public class DataFilesTests
{
    private const string Header = "ID\tName\tType\tIsIgnored\tSize\tDifficulty\tLocations\tValidAI\tParam\tValidIdleAnimIDs\tComments";

    [Fact]
    public void Catalog_rows_are_parsed()
    {
        var catalog = DataFiles.ParseCatalog(
            [Header, "c2500\tHollow\t0\t0\t1\t0\tm10;m11;\t250000;250001\t250000;250010\t7000;7001\tnote", ""],
            7, out var skipped);

        var hollow = Assert.Single(catalog);
        Assert.Empty(skipped);
        Assert.Equal((0, "c2500", EnemyType.Normal, 1, 0), (hollow.Index, hollow.Id, hollow.Type, hollow.Size, hollow.Difficulty));
        Assert.Equal(["m10", "m11"], hollow.Locations);
        Assert.Equal([250000, 250001], hollow.Ais);
        Assert.Equal([250000, 250010], hollow.Params);
        Assert.Equal([7000, 7001], hollow.IdleAnimations);
        Assert.Equal("note", hollow.Comment);
    }

    [Fact]
    public void Rows_with_unequal_ai_and_param_lists_are_skipped_and_indices_stay_dense()
    {
        var catalog = DataFiles.ParseCatalog(
            [Header, "c1\tA\t0\t0\t0\t0\t\t1;2\t1\t-1", "c2\tB\t1\t0\t0\t0\t\t3\t3\t-1"],
            7, out var skipped);

        Assert.Equal(["c1"], skipped);
        Assert.Equal((0, "c2"), (catalog[0].Index, catalog[0].Id));
    }

    [Theory]
    [InlineData("c1\tA\t0\t0\t6\t0\t\t1\t1\t-1")]   // size 6 does not exist
    [InlineData("c1\tA\t0\t0\t0\t8\t\t1\t1\t-1")]   // difficulty above 7
    [InlineData("c1\tA\t0\t0\tx\t0\t\t1\t1\t-1")]   // not a number
    [InlineData("c1\tA\t0")]                         // too few columns
    public void Broken_catalog_rows_are_reported(string row) =>
        Assert.Throws<FormatException>(() => DataFiles.ParseCatalog([Header, row], 7, out _));

    [Fact]
    public void Original_enemies_keep_order_and_ignore_line_endings()
    {
        var enemies = DataFiles.ParseOriginalEnemies(["c2500_0000\t1\r", "c5350_0000\t5", ""]);
        Assert.Equal([new OriginalEnemy("c2500_0000", 1), new OriginalEnemy("c5350_0000", 5)], enemies);
    }

    [Fact]
    public void Ai_references_reverse_their_info_entries()
    {
        var reference = Assert.Single(DataFiles.ParseAiReferences(
            ["120000\tRat\tInu\t120000_battle.lua\t\tA;B\t1;P1;;1;0;0;0|2;P2;S2;0;1;0;0"]));

        Assert.Equal(120000, reference.NpcAi);
        Assert.Equal(["A", "B"], reference.GnlFunctions);
        Assert.Equal([2, 1], reference.InfoEntries.Select(e => e.NpcAi));
        Assert.Equal("", reference.LogicScript);
    }
}

public class EnemyPoolTests
{
    private static EnemyDefinition E(int index, EnemyType type, int size, int difficulty, string ignored = "0") =>
        new(index, "c" + index, "e" + index, type, ignored, size, difficulty, [], [1], [1], [-1], "");

    private static readonly EnemyDefinition[] Catalog =
    [
        E(0, EnemyType.Normal, 0, 0),
        E(1, EnemyType.Normal, 3, 2),
        E(2, EnemyType.Boss, 5, 6),
        E(3, EnemyType.Npc, 1, 1),
        E(4, EnemyType.Boss, 2, 6, ignored: "2"),   // Remastered only
        E(5, EnemyType.Normal, 0, 0, ignored: "1"), // disabled
    ];

    [Fact]
    public void Size_lookup_includes_smaller_enemies()
    {
        var pool = EnemyPool.Build(Catalog, GameEdition.Ptde, hostileNpcs: false, maxDifficulty: 7);

        Assert.Equal([0], pool.BySize(PoolKind.Normal, 2));
        Assert.Equal([0, 1], pool.BySize(PoolKind.Normal, 3));
        Assert.Empty(pool.BySize(PoolKind.Boss, 4));
        Assert.Equal([2], pool.Bosses);
    }

    [Fact]
    public void Disabled_and_remastered_only_enemies_follow_the_edition()
    {
        Assert.DoesNotContain(4, EnemyPool.Build(Catalog, GameEdition.Ptde, false, 7).Bosses);
        Assert.Equal([2, 4], EnemyPool.Build(Catalog, GameEdition.Remastered, false, 7).Bosses);
        Assert.DoesNotContain(5, EnemyPool.Build(Catalog, GameEdition.Remastered, false, 7).BySize(PoolKind.Any, 5));
    }

    [Fact]
    public void Npcs_are_normal_enemies_only_when_hostile()
    {
        Assert.Equal([0, 1], EnemyPool.Build(Catalog, GameEdition.Ptde, false, 7).Normals);
        Assert.Equal([0, 1, 3], EnemyPool.Build(Catalog, GameEdition.Ptde, true, 7).Normals);
        Assert.Contains(3, EnemyPool.Build(Catalog, GameEdition.Ptde, false, 7).BySize(PoolKind.Any, 5));
    }

    [Fact]
    public void Difficulty_lookups()
    {
        var pool = EnemyPool.Build(Catalog, GameEdition.Ptde, false, 7);

        Assert.Equal([1], pool.ByDifficulty(PoolKind.Normal, 2));
        Assert.Empty(pool.ByDifficultyAndSize(PoolKind.Normal, 2, 2));
        Assert.Equal([1], pool.ByDifficultyAndSize(PoolKind.Normal, 2, 5));
        Assert.Equal([2], pool.ByDifficultyAndSize(PoolKind.Boss, 6, 5));
        Assert.Equal(8, pool.DifficultyClasses);
    }

    [Fact]
    public void Replacement_targets_match_by_substring_first_entry_wins()
    {
        var targets = new ReplacementTargets([new("c2500", "Hollow", EnemyType.Normal, 0), new("c2500_0001", "Special", EnemyType.Normal, 3)]);

        Assert.Equal("Hollow", targets.Find("c2500_0001")!.Name);
        Assert.Null(targets.Find("c9999_0000"));
    }
}
