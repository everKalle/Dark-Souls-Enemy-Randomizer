using System.Security.Cryptography;
using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Installation;
using EnemyRandomizer.Infrastructure.Game;
using EnemyRandomizer.Infrastructure.Installation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Tests.Installation;

public sealed class InstallationTests : IDisposable
{
    private static readonly byte[] SearchBytes = [0x68, 0x00, 0x00, 0xA2, 0x00];
    private static readonly byte[] PatchedBytes = [0x68, 0x00, 0x00, 0xDA, 0x00];

    private readonly TempGameFolder _folder = new();
    private readonly GameDataSettings _gameData = ShippedSettings.Get<GameDataSettings>();
    private readonly GameFileLayout _layout;

    public InstallationTests() => _layout = new GameFileLayout(Options.Create(_gameData));

    public void Dispose() => _folder.Dispose();

    private GameInstallation Installation() => new(_folder.Paths, Options.Create(_gameData));

    private InstallationStatus Status(GameInstallation installation) => new(
        installation,
        new CompositeInstallationCheck(
        [
            new ExeCheck(),
            new GameFilesCheck(_folder.Paths, _layout),
            new DataFilesCheck(_folder.Paths, _layout, Options.Create(_folder.Settings)),
            new WritePermissionCheck(_folder.Paths, _layout),
        ]),
        new ExePatcher(_folder.Paths, installation, Options.Create(_gameData), NullLogger<ExePatcher>.Instance),
        new global::EnemyRandomizer.Infrastructure.Setup.FirstTimeSetup(_folder.Paths, _layout, Options.Create(_gameData), ShippedSettings.Options<MsbModelSettings>(), NullLogger<global::EnemyRandomizer.Infrastructure.Setup.FirstTimeSetup>.Instance),
        _folder.Paths,
        Options.Create(_folder.Settings),
        NullLogger<InstallationStatus>.Instance);

    private Task<InstallationReport> Check(GameInstallation installation) =>
        Status(installation).CheckAsync(new Progress<Core.Abstractions.OperationProgress>(), TestContext.Current.CancellationToken);

    /// <summary>A fake PTDE exe whose hash is registered as <paramref name="state"/>.</summary>
    private byte[] FakeExe(ExeState state, byte[]? content = null)
    {
        content ??= [.. "MZ"u8, .. SearchBytes, 1, 2, .. SearchBytes, 3];
        _folder.Touch("DARKSOULS.exe", content);
        _gameData.Editions[GameEdition.Ptde].ExeHashes[Convert.ToHexStringLower(SHA256.HashData(content))] = state;
        return content;
    }

    [Fact]
    public void No_exe_is_not_found()
    {
        var installation = Installation();
        Assert.Equal(ExeState.NotFound, installation.ExeState);
        Assert.Null(installation.Edition);
    }

    [Fact]
    public void Remastered_exe_is_detected_by_name()
    {
        _folder.Touch("DarkSoulsRemastered.exe");
        Assert.Equal(GameEdition.Remastered, Installation().Edition);
    }

    [Fact]
    public void Ptde_exe_with_unknown_hash_is_unknown()
    {
        _folder.Touch("DARKSOULS.exe", [9, 9, 9]);
        var installation = Installation();
        Assert.Equal(ExeState.Unknown, installation.ExeState);
        Assert.Equal(GameEdition.Ptde, installation.Edition);
    }

    [Fact]
    public void Ptde_exe_hash_maps_to_state()
    {
        FakeExe(ExeState.NotUnpacked);
        Assert.Equal(ExeState.NotUnpacked, Installation().ExeState);
    }

    [Fact]
    public async Task Complete_remastered_install_has_no_problems()
    {
        _folder.Touch("DarkSoulsRemastered.exe");
        _folder.CreateGameFiles(GameEdition.Remastered, _layout);
        _folder.CopyShippedData();

        var report = (await Check(Installation()));

        Assert.Empty(report.Problems);
        Assert.True(report.CanRandomize);
    }

    [Fact]
    public async Task Missing_files_are_reported_per_folder()
    {
        _folder.Touch("DarkSoulsRemastered.exe");
        _folder.CreateGameFiles(GameEdition.Remastered, _layout);
        _folder.CopyShippedData();
        File.Delete(_folder.Paths.InGame(_layout.AiScript("m10_00_00_00", GameEdition.Remastered)));
        Directory.Delete(_folder.Paths.InGame("event"), recursive: true);

        var problems = (await Check(Installation())).Problems;

        Assert.Equal(MessageKeys.GameFilesMissingRemastered, problems[0].MessageKey);
        Assert.Contains(problems, p => p.MessageKey == MessageKeys.FilesMissingInFolder
                                       && p.Arguments.SequenceEqual(new object[] { 1, 17, ".luabnd.dcx", "script" }));
        Assert.Contains(problems, p => p.MessageKey == MessageKeys.NoFilesInFolder && Equals(p.Arguments[0], "event"));
    }

    [Fact]
    public async Task Missing_data_folder_is_reported_once()
    {
        Directory.Delete(_folder.Paths.DataDirectory, recursive: true);
        _folder.Touch("DarkSoulsRemastered.exe");
        _folder.CreateGameFiles(GameEdition.Remastered, _layout);

        var problems = (await Check(Installation())).Problems;

        Assert.Equal([MessageKeys.DataFolderMissing], problems.Select(p => p.MessageKey));
    }

    [Fact]
    public async Task Missing_reference_files_are_counted()
    {
        _folder.Touch("DarkSoulsRemastered.exe");
        _folder.CreateGameFiles(GameEdition.Remastered, _layout);
        _folder.CopyShippedData();
        File.Delete(Path.Combine(_folder.Paths.OriginalEnemyReferenceDirectory, "m12_01_00_00.ptde.txt"));

        var problem = Assert.Single((await Check(Installation())).Problems);

        Assert.Equal(MessageKeys.OriginalReferencesMissing, problem.MessageKey);
        Assert.Equal(1, problem.Arguments[0]);
        Assert.Equal(20, problem.Arguments[1]);
    }

    [Fact]
    public async Task Read_only_map_file_is_reported()
    {
        _folder.Touch("DarkSoulsRemastered.exe");
        _folder.CreateGameFiles(GameEdition.Remastered, _layout);
        _folder.CopyShippedData();
        File.SetAttributes(_folder.Paths.InGame(_layout.MapStudio("m10_00_00_00")), FileAttributes.ReadOnly);

        var problem = Assert.Single((await Check(Installation())).Problems);

        Assert.Equal(MessageKeys.NoWritePermission, problem.MessageKey);
    }

    [Fact]
    public async Task Unpacked_ptde_exe_is_backed_up_and_patched_at_startup()
    {
        var original = FakeExe(ExeState.Unpacked);
        byte[] patched = [.. "MZ"u8, .. PatchedBytes, 1, 2, .. PatchedBytes, 3];
        _gameData.Editions[GameEdition.Ptde].ExeHashes[Convert.ToHexStringLower(SHA256.HashData(patched))] = ExeState.Patched;
        _folder.CreateGameFiles(GameEdition.Ptde, _layout);
        _folder.CopyShippedData();

        var report = (await Check(Installation()));

        Assert.True(report.CanRandomize);
        Assert.Equal(ExeState.Patched, report.ExeState);
        Assert.Equal(patched, File.ReadAllBytes(_folder.Paths.InGame("DARKSOULS.exe")));
        Assert.Equal(original, File.ReadAllBytes(_folder.Paths.InGame("DARKSOULS.prerandomizer.exe")));
    }

    [Fact]
    public void Patched_exe_can_be_restored()
    {
        var original = FakeExe(ExeState.Unpacked);
        var installation = Installation();
        var patcher = new ExePatcher(_folder.Paths, installation, Options.Create(_gameData), NullLogger<ExePatcher>.Instance);
        Assert.True(patcher.PatchIfNeeded());
        _gameData.Editions[GameEdition.Ptde].ExeHashes[GameInstallation.Sha256(_folder.Paths.InGame("DARKSOULS.exe"))] = ExeState.Patched;
        installation.Refresh();

        Assert.True(patcher.Restore());

        Assert.Equal(original, File.ReadAllBytes(_folder.Paths.InGame("DARKSOULS.exe")));
        Assert.Equal(ExeState.Unpacked, installation.ExeState);
    }

    [Fact]
    public async Task Not_unpacked_exe_blocks_and_is_not_patched()
    {
        var original = FakeExe(ExeState.NotUnpacked);
        _folder.CreateGameFiles(GameEdition.Ptde, _layout);
        _folder.CopyShippedData();

        var report = (await Check(Installation()));

        Assert.Equal([MessageKeys.ExeNotUnpacked], report.Problems.Select(p => p.MessageKey));
        Assert.Equal(original, File.ReadAllBytes(_folder.Paths.InGame("DARKSOULS.exe")));
    }

    [Fact]
    public void Replace_all_replaces_every_occurrence()
    {
        byte[] data = [1, 2, 3, 1, 2, 3, 1, 2];

        var count = ExePatcher.ReplaceAll(data, [1, 2], [7, 8]);

        Assert.Equal(3, count);
        Assert.Equal(new byte[] { 7, 8, 3, 7, 8, 3, 7, 8 }, data);
    }
}
