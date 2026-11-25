using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Installation;
using EnemyRandomizer.Formats.Bnd;
using EnemyRandomizer.Formats.Dcx;
using EnemyRandomizer.Formats.Msb;
using EnemyRandomizer.Infrastructure.Game;
using EnemyRandomizer.Infrastructure.Installation;
using EnemyRandomizer.Infrastructure.Setup;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Tests.Setup;

public sealed class SetupTests : IDisposable
{
    private readonly TempGameFolder _folder = new();
    private readonly GameDataSettings _gameData = ShippedSettings.Get<GameDataSettings>();
    private readonly MsbModelSettings _models = ShippedSettings.Get<MsbModelSettings>();
    private readonly GameFileLayout _layout;
    private readonly FirstTimeSetup _setup;

    public SetupTests()
    {
        _layout = new GameFileLayout(Options.Create(_gameData));
        _setup = new FirstTimeSetup(_folder.Paths, _layout, Options.Create(_gameData), Options.Create(_models), NullLogger<FirstTimeSetup>.Instance);
    }

    public void Dispose() => _folder.Dispose();

    private static IProgress<OperationProgress> NoProgress => new Progress<OperationProgress>();

    private string Game(string relative) => _folder.Paths.InGame(relative);

    private static byte[] Unpack(string file)
    {
        var bytes = File.ReadAllBytes(file);
        return DcxFile.IsDcx(bytes) ? DcxFile.Read(bytes).Data : bytes;
    }

    private SetupResult Run(GameEdition edition) => _setup.Run(edition, NoProgress, TestContext.Current.CancellationToken);

    [Theory]
    [InlineData(GameEdition.Ptde)]
    [InlineData(GameEdition.Remastered)]
    public void Setup_creates_all_copies(GameEdition edition)
    {
        _folder.CreateGameFiles(edition, _layout);

        var result = Run(edition);

        Assert.True(result.CopiesValid);
        Assert.False(result.EffectsFailed);
        Assert.Equal(18, result.Maps);
        Assert.Equal(17, result.AiArchives);
        Assert.True(File.Exists(Path.Combine(_folder.Paths.AiScriptDirectory, "120000_battle.lua")));
        Assert.True(File.Exists(Path.Combine(_folder.Paths.AiScriptDirectory, "540000_battle.lua")));
        Assert.True(Directory.Exists(_folder.Paths.LogDirectory));
        Assert.True(Directory.Exists(_folder.Paths.LayoutReferenceDirectory));
        Assert.True(File.Exists(Path.Combine(_folder.Paths.ParamBackupDirectory, Path.GetFileName(_layout.GameParam(edition)))));
    }

    [Fact]
    public void Map_copies_get_every_character_model_after_the_last_one()
    {
        _folder.CreateGameFiles(GameEdition.Ptde, _layout);

        Run(GameEdition.Ptde);

        var copy = MsbFile.Read(File.ReadAllBytes(_setup.MapCopy("m10_00_00_00")));
        var added = copy.Models.Rows.Skip(2).ToList();
        Assert.Equal(_models.AllModels, added.Select(r => r.Name));
        Assert.Equal(5, added[0].GetInt("Index"));               // last character model had index 4
        Assert.All(added, r => Assert.Equal(2, r.GetInt("Type")));
        Assert.Equal(@"N:\FRPG\data\Model\chr\c1200\sib\c1200.sib", added[0].Values[added[0].Layout.NameIndex + 1]);
    }

    [Fact]
    public void Copies_are_made_from_the_backup_when_one_exists()
    {
        _folder.CreateGameFiles(GameEdition.Ptde, _layout);
        var map = Game(_layout.MapStudio("m10_00_00_00"));
        File.Copy(map, BackupFiles.BackupOf(map));
        var randomized = new MsbFile();
        randomized.Models.Add(32, 2, 0, 38, 1, 0, 0, 0, "c9999", "x");
        File.WriteAllBytes(map, randomized.Write());

        Run(GameEdition.Ptde);

        Assert.Equal("m0000", MsbFile.Read(File.ReadAllBytes(_setup.MapCopy("m10_00_00_00"))).Models.Rows[0].Name);
    }

    [Fact]
    public void Running_again_changes_nothing()
    {
        _folder.CreateGameFiles(GameEdition.Remastered, _layout);
        Run(GameEdition.Remastered);
        var common = Game(_layout.Sfx("CommonEffects", GameEdition.Remastered));
        var before = File.GetLastWriteTimeUtc(common);
        var copyBefore = File.ReadAllBytes(_setup.MapCopy("m10_00_00_00"));

        Run(GameEdition.Remastered);

        Assert.Equal(before, File.GetLastWriteTimeUtc(common));
        Assert.Equal(copyBefore, File.ReadAllBytes(_setup.MapCopy("m10_00_00_00")));
    }

    [Theory]
    [InlineData(GameEdition.Ptde)]
    [InlineData(GameEdition.Remastered)]
    public void Effects_are_merged_into_common_effects(GameEdition edition)
    {
        _folder.CreateGameFiles(edition, _layout);

        Run(edition);

        var common = Bnd3.Read(Unpack(Game(_layout.Sfx("CommonEffects", edition))));
        Assert.Equal(1 + 23, common.Count);                  // its own entry + one new effect per map bundle
        Assert.True(File.Exists(BackupFiles.BackupOf(Game(_layout.Sfx("CommonEffects", edition)))));

        var mapBundle = Bnd3.Read(Unpack(Game(_layout.Sfx("m10", edition))));
        Assert.Equal(edition == GameEdition.Ptde ? 1 : 2, mapBundle.Count); // PTDE map bundles keep the first entry only
    }

    [Fact]
    public void Verify_reports_broken_copies()
    {
        _folder.CreateGameFiles(GameEdition.Ptde, _layout);
        Run(GameEdition.Ptde);
        File.WriteAllBytes(_setup.MapCopy("m10_00_00_00"), [1]);
        File.Delete(_setup.AiCopy("m11_00_00_00", GameEdition.Ptde));

        var result = _setup.Verify(GameEdition.Ptde);

        Assert.Equal((0, 1, 1, 0), (result.MissingMapCopies, result.InvalidMapCopies, result.MissingAiCopies, result.InvalidAiCopies));
        Assert.False(result.CopiesValid);
    }

    [Fact]
    public void Revert_restores_backups_and_npc_param()
    {
        _folder.CreateGameFiles(GameEdition.Remastered, _layout);
        Run(GameEdition.Remastered);
        var map = Game(_layout.MapStudio("m10_00_00_00"));
        var original = File.ReadAllBytes(map);
        BackupFiles.WriteWithBackup(map, [9, 9, 9]);

        var param = Game(_layout.GameParam(GameEdition.Remastered));
        var archive = GameArchive.Read(param);
        archive.Entries[12] = archive.Entries[12] with { Data = [7, 7] };
        archive.Entries[3] = archive.Entries[3] with { Data = [3, 3] };       // other params stay as they are
        File.WriteAllBytes(param, archive.ToBytes());

        var restorer = new GameRestorer(_folder.Paths, _layout, new NoExePatcher(), Options.Create(_gameData), NullLogger<GameRestorer>.Instance);
        var result = restorer.Revert(GameEdition.Remastered, restoreEffects: false, NoProgress, TestContext.Current.CancellationToken);

        Assert.Equal(original, File.ReadAllBytes(map));
        Assert.True(result.ParamRestored);
        var reverted = GameArchive.Read(param).Entries;
        Assert.Equal([12, 0, 0, 0], reverted[12].Data);
        Assert.Equal([3, 3], reverted[3].Data);
        Assert.Equal(1 + 23, Bnd3.Read(Unpack(Game(_layout.Sfx("CommonEffects", GameEdition.Remastered)))).Count); // merged effects stay
    }

    [Fact]
    public void Revert_with_effects_restores_effect_files()
    {
        _folder.CreateGameFiles(GameEdition.Ptde, _layout);
        var common = Game(_layout.Sfx("CommonEffects", GameEdition.Ptde));
        var original = File.ReadAllBytes(common);
        Run(GameEdition.Ptde);
        Assert.NotEqual(original, File.ReadAllBytes(common));

        var restorer = new GameRestorer(_folder.Paths, _layout, new NoExePatcher(), Options.Create(_gameData), NullLogger<GameRestorer>.Instance);
        restorer.Revert(GameEdition.Ptde, restoreEffects: true, NoProgress, TestContext.Current.CancellationToken);

        Assert.Equal(original, File.ReadAllBytes(common));
        Assert.Equal(2, Bnd3.Read(File.ReadAllBytes(Game(_layout.Sfx("m10", GameEdition.Ptde)))).Count);
    }

    [Fact]
    public async Task Startup_runs_setup_after_successful_checks()
    {
        _folder.Touch("DarkSoulsRemastered.exe");
        _folder.CreateGameFiles(GameEdition.Remastered, _layout);
        _folder.CopyShippedData();
        var installation = new GameInstallation(_folder.Paths, Options.Create(_gameData));
        var status = new InstallationStatus(
            installation,
            new CompositeInstallationCheck([new ExeCheck(), new GameFilesCheck(_folder.Paths, _layout)]),
            new NoExePatcher(),
            _setup,
            _folder.Paths,
            Options.Create(_folder.Settings),
            NullLogger<InstallationStatus>.Instance);

        var report = await status.CheckAsync(NoProgress, TestContext.Current.CancellationToken);

        Assert.True(report.CanRandomize);
        Assert.False(report.CopiesFailed);
        Assert.Same(report, status.Report);
        Assert.True(File.Exists(_setup.MapCopy("m18_01_00_00")));
    }

    private sealed class NoExePatcher : IExePatcher
    {
        public bool PatchIfNeeded() => true;

        public bool Restore() => false;
    }
}
