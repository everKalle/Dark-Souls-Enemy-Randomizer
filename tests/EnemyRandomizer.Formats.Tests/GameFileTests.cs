using EnemyRandomizer.Formats.Bnd;
using EnemyRandomizer.Formats.Dcx;
using EnemyRandomizer.Formats.Lua;
using EnemyRandomizer.Formats.Msb;
using EnemyRandomizer.Formats.Param;

namespace EnemyRandomizer.Formats.Tests;

/// <summary>
/// Round trips on real game files. Skipped unless <c>DS_GAME_DIR</c> points to an (unpacked PTDE or Remastered)
/// game folder. Files are only read, never written.
/// </summary>
public class GameFileTests
{
    private static string GameDir
    {
        get
        {
            var dir = Environment.GetEnvironmentVariable("DS_GAME_DIR");
            Assert.SkipWhen(string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir), "DS_GAME_DIR is not set to a game folder.");
            return dir!;
        }
    }

    private static byte[] Unpack(string file)
    {
        var bytes = File.ReadAllBytes(file);
        return DcxFile.IsDcx(bytes) ? DcxFile.Read(bytes).Data : bytes;
    }

    [Fact]
    public void Every_map_survives_read_and_write()
    {
        var files = Directory.GetFiles(Path.Combine(GameDir, "map", "MapStudio"), "m*.msb");
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var msb = MsbFile.Read(File.ReadAllBytes(file));
            var written = msb.Write();
            var reread = MsbFile.Read(written);

            Assert.Equal(msb.Creatures.Rows.Select(r => r.Values), reread.Creatures.Rows.Select(r => r.Values));
            Assert.Equal(msb.Models.Rows.Count, reread.Models.Rows.Count);
            Assert.Equal(written, reread.Write());
        }
    }

    [Fact]
    public void Every_ai_archive_survives_read_and_write()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(GameDir, "script"), "m*.luabnd*").Where(f => !f.EndsWith(".bak", StringComparison.Ordinal)))
        {
            var bnd = LuaBnd.Read(File.ReadAllBytes(file));
            var gnl = LuaGnl.Read(bnd.GnlData);
            var info = LuaInfo.Read(bnd.InfoData);

            Assert.Equal(bnd.GnlData, gnl.Write());
            Assert.Equal(bnd.InfoData, info.Write());
            Assert.Equal(bnd.Entries.Count, LuaBnd.Read(bnd.Write(gnl.Write(), info.Write())).Entries.Count);
        }
    }

    [Fact]
    public void Npc_param_survives_read_and_write()
    {
        var file = Directory.GetFiles(Path.Combine(GameDir, "param", "GameParam"), "GameParam.parambnd*")
                            .First(f => !f.EndsWith(".bak", StringComparison.Ordinal));
        var entries = Bnd3.Read(Unpack(file));
        var npcEntry = entries[12];
        Assert.Contains("NpcParam", npcEntry.Name, StringComparison.Ordinal);

        var param = NpcParam.Read(npcEntry.Data);
        Assert.Equal(param.Schema.RowSize, param.File.DataSize);

        var written = param.Write();
        Assert.Equal(written, NpcParam.Read(written).Write());
        Assert.Equal(param.File.Rows.Select(r => r.Data), NpcParam.Read(written).File.Rows.Select(r => r.Data));
    }
}
