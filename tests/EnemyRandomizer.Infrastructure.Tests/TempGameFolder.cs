using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Formats.Bnd;
using EnemyRandomizer.Formats.Dcx;
using EnemyRandomizer.Formats.Msb;
using EnemyRandomizer.Infrastructure.Game;

namespace EnemyRandomizer.Infrastructure.Tests;

/// <summary>A throw-away game folder with the shipped <c>enemyRandomizerData</c> catalog.</summary>
internal sealed class TempGameFolder : IDisposable
{
    public TempGameFolder()
    {
        Root = Path.Combine(Path.GetTempPath(), "er-tests-" + Guid.NewGuid().ToString("N"));
        Settings = new AppSettings
        {
            Title = "t",
            OriginalAuthor = "a",
            ProjectUrl = "https://example.org",
            GameDirectory = Root,
            DataFolder = "enemyRandomizerData",
            FileCopyRetries = 1,
            RequiredDataFiles = ["airef.csv", "replacement_ref/valid_new.txt", "replacement_ref/valid_replacements.txt"],
            DataFolders = new DataFolderSettings
            {
                MapStudioCopies = "mapStudioCopies", MapAiCopies = "mapAiCopies", SfxCopies = "sfxCopies",
                AiScripts = "aiscripts", ParamBackup = "param", Logs = "logs", LayoutReferences = "refs",
                CustomConfigs = "customConfigs", Emevd = "emevd", ReplacementReferences = "replacement_ref",
                OriginalEnemyReferences = "original_enemies_ref",
            },
        };
        Paths = new GamePaths(Settings, Root);

        Directory.CreateDirectory(Paths.ReplacementReferenceDirectory);
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "data", "valid_new.txt"),
            Path.Combine(Paths.ReplacementReferenceDirectory, "valid_new.txt"));
    }

    public string Root { get; }

    /// <summary>Creates a file relative to the game folder.</summary>
    public string Touch(string relativePath, byte[]? content = null)
    {
        var file = Paths.InGame(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllBytes(file, content ?? [1, 2, 3]);
        return file;
    }

    /// <summary>Copies the shipped reference files from <c>dist/enemyRandomizerData</c>.</summary>
    public void CopyShippedData()
    {
        var source = Path.Combine(ShippedSettings.RepositoryRoot, "dist", "enemyRandomizerData");
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(Paths.DataDirectory, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    /// <summary>Creates every game file the startup checks look for.</summary>
    /// <summary>
    /// Creates minimal but valid game files: maps with 2 models, AI archives with one script each
    /// (120000/540000 in the first two maps), effect bundles with 2 entries, a GameParam with 13 params.
    /// Remastered files are DCX-compressed.
    /// </summary>
    public void CreateGameFiles(GameEdition edition, GameFileLayout layout)
    {
        byte[] Pack(byte[] content) => edition == GameEdition.Remastered ? DcxFile.Write(DcxHeader.Emevd, content) : content;

        foreach (var map in layout.Maps)
        {
            var msb = new MsbFile();
            msb.Models.Add(32, 0, 0, 38, 1, 0, 0, 0, "m0000", "map.sib");
            msb.Models.Add(32, 2, 4, 38, 1, 0, 0, 0, "c2500", "chr.sib");
            Touch(layout.MapStudio(map.Id), msb.Write());
        }

        var randomized = layout.RandomizedMaps.ToList();
        for (var i = 0; i < randomized.Count; i++)
        {
            var scriptId = i switch { 0 => 120000, 1 => 540000, _ => 200000 + i };
            Touch(layout.AiScript(randomized[i].Id, edition), Pack(Bnd3.Write(
            [
                new BndEntry(1, $@"N:\FRPG\data\INTERROOT_win32\script\ai\out\bin\{scriptId}_battle.lua", [(byte)i, 1, 2]),
                new BndEntry(1000000, "x.luagnl", [0, 0, 0, 0, 0]),
                new BndEntry(1000001, "x.luainfo", [.. "LUAI"u8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
            ])));
            Touch(layout.Event(randomized[i].Id, edition), Pack([.. "EVD\0"u8, 1, 2, 3, 4, 5, 6]));
        }

        foreach (var (ffx, i) in layout.FfxFiles.Select((f, i) => (f, i)))
        {
            List<BndEntry> entries = ffx == "CommonEffects"
                ? [new BndEntry(1, "f0001000.ffx", [1])]
                : [new BndEntry(1, $"f00{10000 + i}.ffx", [(byte)i]), new BndEntry(2, "f0001000.ffx", [1])];
            Touch(layout.Sfx(ffx, edition), Pack(Bnd3.Write(entries)));
        }

        Touch(layout.GameParam(edition), Pack(Bnd3.Write(
            [.. Enumerable.Range(0, 13).Select(i => new BndEntry(i, $"param{i}.param", [(byte)i, 0, 0, 0]))])));
    }

    public AppSettings Settings { get; }

    public IGamePaths Paths { get; }

    public void Dispose()
    {
        if (!Directory.Exists(Root))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal); // read-only files from permission tests
        }

        Directory.Delete(Root, recursive: true);
    }
}

internal sealed class FixedInstallation(GameEdition? edition) : IGameInstallation
{
    public GameEdition? Edition { get; } = edition;

    public ExeState ExeState => Edition switch
    {
        GameEdition.Ptde => ExeState.Patched,
        GameEdition.Remastered => ExeState.Remastered,
        _ => ExeState.NotFound,
    };

    public void Refresh()
    {
    }
}
