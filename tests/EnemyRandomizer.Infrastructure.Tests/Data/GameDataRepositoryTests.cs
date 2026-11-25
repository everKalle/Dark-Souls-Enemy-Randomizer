using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Data;
using EnemyRandomizer.Infrastructure.Data;
using EnemyRandomizer.Infrastructure.Game;
using EnemyRandomizer.Infrastructure.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Tests.Data;

/// <summary>Loads the data files shipped in <c>dist/enemyRandomizerData</c>.</summary>
public sealed class GameDataRepositoryTests : IDisposable
{
    private readonly TempGameFolder _folder = new();
    private readonly GameDataSettings _gameData = ShippedSettings.Get<GameDataSettings>();
    private readonly GameDataRepository _repository;

    public GameDataRepositoryTests()
    {
        _folder.CopyShippedData();
        _repository = new GameDataRepository(
            _folder.Paths,
            new GameFileLayout(Options.Create(_gameData)),
            ShippedSettings.Options<DifficultyCurveSettings>(),
            NullLogger<GameDataRepository>.Instance);
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Shipped_catalog_loads_completely()
    {
        var catalog = _repository.LoadCatalog("Default");

        Assert.Equal(123, catalog.Count);
        Assert.Equal(Enumerable.Range(0, 123), catalog.Select(e => e.Index));
        Assert.Contains(catalog, e => e.Type == EnemyType.Boss);
        Assert.Contains(catalog, e => e.Type == EnemyType.Npc);
    }

    [Fact]
    public void Easy_asylum_indices_point_to_the_expected_enemy_types()
    {
        var catalog = _repository.LoadCatalog("Default");
        var easy = ShippedSettings.Get<MapPatchSettings>().EasyAsylum;

        Assert.All(easy.NormalEnemyIndices, i => Assert.Equal(EnemyType.Normal, catalog[i].Type));
        Assert.All(easy.BossIndices, i => Assert.Equal(EnemyType.Boss, catalog[i].Type));
    }

    [Fact]
    public void Every_ai_of_an_enabled_enemy_has_an_ai_reference()
    {
        var missing = _repository.LoadCatalog("Default")
            .Where(e => e.IsEnabledOn(GameEdition.Remastered))
            .SelectMany(e => e.Ais.Select(ai => (e.Id, ai)))
            .Where(x => !_repository.AiReferences.ContainsKey(x.ai))
            .ToList();

        Assert.Empty(missing);
    }

    [Theory]
    [InlineData(GameEdition.Ptde)]
    [InlineData(GameEdition.Remastered)]
    public void Original_enemies_load_for_every_map(GameEdition edition)
    {
        foreach (var map in _gameData.Maps)
        {
            var enemies = _repository.LoadOriginalEnemies(map.Id, edition);
            Assert.NotEmpty(enemies);
            Assert.All(enemies, e => Assert.InRange(e.Size, 0, EnemyPool.SizeClasses - 1));
        }
    }

    [Fact]
    public void Edition_specific_reference_files_differ()
    {
        var ptde = _repository.LoadOriginalEnemies("m12_01_00_00", GameEdition.Ptde);
        var remastered = _repository.LoadOriginalEnemies("m12_01_00_00", GameEdition.Remastered);
        Assert.NotEqual(ptde, remastered);
    }

    [Fact]
    public void Every_replacement_target_is_parsed()
    {
        var targets = _repository.ReplacementTargets;

        Assert.True(targets.All.Count > 100);
        Assert.Equal("Rat", targets.Find("c1200_0000")!.Name);
    }

    [Fact]
    public void Pool_uses_a_custom_enemy_config()
    {
        var configs = new EnemyConfigRepository(_folder.Paths, new FixedInstallation(GameEdition.Ptde));
        var flags = configs.Load("Default").Select(e => e.Id == "c2500").ToList();
        configs.Save("Only hollows", flags);

        var pool = _repository.LoadPool("Only hollows", GameEdition.Ptde, hostileNpcs: false);

        Assert.Equal(["c2500"], pool.Normals.Select(i => pool[i].Id));
        Assert.Empty(pool.Bosses);
    }

    [Fact]
    public void Default_pool_has_normals_and_bosses_in_every_size()
    {
        var pool = _repository.LoadPool("Default", GameEdition.Remastered, hostileNpcs: false);

        Assert.NotEmpty(pool.BySize(PoolKind.Normal, 0));
        Assert.NotEmpty(pool.BySize(PoolKind.Boss, EnemyPool.SizeClasses - 1));
        Assert.True(pool.Normals.Count > pool.Bosses.Count);
    }
}
