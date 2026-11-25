using System.Text;
using EnemyRandomizer.Formats.Bnd;
using EnemyRandomizer.Formats.Dcx;
using EnemyRandomizer.Formats.Lua;

namespace EnemyRandomizer.Formats.Tests;

public class LuaGnlTests
{
    private static byte[] B(string s) => Encoding.ASCII.GetBytes(s);

    private static LuaGnl Sample()
    {
        var gnl = new LuaGnl();
        gnl.FunctionNames.AddRange([B("Alpha"), B("Beta"), B("")]);
        gnl.UpdateOffsets();
        return gnl;
    }

    [Fact]
    public void Offsets_point_to_names_and_end_with_zero()
    {
        var gnl = Sample();
        Assert.Equal([12u, 18u, 0u], gnl.Offsets);
    }

    [Fact]
    public void Write_then_read_round_trips()
    {
        var bytes = Sample().Write();

        var read = LuaGnl.Read(bytes);

        Assert.Equal(bytes, read.Write());
        Assert.Equal(["Alpha", "Beta", ""], read.FunctionNames.Select(Encoding.ASCII.GetString));
    }

    [Fact]
    public void Add_entries_inserts_at_start()
    {
        var gnl = Sample();
        gnl.AddEntriesAuto([B("New1"), B("New2")]);

        Assert.Equal(["New1", "New2", "Alpha", "Beta", ""], gnl.FunctionNames.Select(Encoding.ASCII.GetString));
        Assert.Equal(0u, gnl.Offsets[^1]);
        Assert.Equal(20u, gnl.Offsets[0]);
    }
}

public class LuaInfoTests
{
    private static byte[] B(string s) => Encoding.ASCII.GetBytes(s);

    private static LuaInfo Sample()
    {
        var info = new LuaInfo { Unknown = 1, Separator = 0 };
        info.FunctionNames.AddRange([B("main_battle"), B("main_logic")]);
        info.Entries.Add(new LuaInfoEntry { NpcAi = 100, PrimaryRef = 0, SecondaryRef = 1, FlagA = 1 });
        return info;
    }

    [Fact]
    public void Write_then_read_round_trips()
    {
        var bytes = Sample().Write();

        var read = LuaInfo.Read(bytes);

        Assert.Equal(bytes, read.Write());
        Assert.Equal(0, read.Entries[0].PrimaryRef);
        Assert.Equal(1, read.Entries[0].SecondaryRef);
        Assert.Equal(1, read.Entries[0].FlagA);
    }

    [Fact]
    public void Add_entry_auto_prepends_names_and_shifts_refs()
    {
        var info = Sample();

        var existed = info.AddEntryAuto([new LuaInfoValue(200, B("p"), B("s"), 1, 0, 0, 0)]);

        Assert.False(existed);
        Assert.Equal(["p", "s", "main_battle", "main_logic"], info.FunctionNames.Select(Encoding.ASCII.GetString));
        Assert.Equal((200u, 0, 1), (info.Entries[0].NpcAi, info.Entries[0].PrimaryRef, info.Entries[0].SecondaryRef));
        Assert.Equal((2, 3), (info.Entries[1].PrimaryRef, info.Entries[1].SecondaryRef));
    }

    [Fact]
    public void Existing_ai_is_not_added_twice()
    {
        var info = Sample();
        Assert.True(info.AddEntryAuto([new LuaInfoValue(100, B("p"), B("s"), 1, 0, 0, 0)]));
        Assert.Single(info.Entries);
    }
}

public class LuaBndTests
{
    private static byte[] Archive() => Bnd3.Write(
    [
        new(3, @"N:\FRPG\data\INTERROOT_win32\script\ai\out\bin\123400_battle.lua", [1]),
        new(LuaBnd.GnlId, "m10.luagnl", [2]),
        new(LuaBnd.InfoId, "m10.luainfo", [3]),
    ]);

    [Fact]
    public void Exposes_gnl_info_and_max_id()
    {
        var bnd = LuaBnd.Read(Archive());

        Assert.Equal([2], bnd.GnlData);
        Assert.Equal([3], bnd.InfoData);
        Assert.Equal(3, bnd.MaxScriptId);
        Assert.Null(bnd.Dcx);
        Assert.Equal(["123400_battle.lua"], bnd.AiScripts().Select(s => s.FileName));
    }

    [Fact]
    public void Add_inserts_before_gnl_and_info()
    {
        var bnd = LuaBnd.Read(Archive());

        bnd.Add("555500_logic.lua", [9]);

        Assert.Equal([3, 4, LuaBnd.GnlId, LuaBnd.InfoId], bnd.Entries.Select(e => e.Id));
        Assert.EndsWith(@"INTERROOT_win32\script\ai\out\bin\555500_logic.lua", bnd.Entries[1].Name, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_replaces_gnl_info_and_keeps_dcx()
    {
        var dcx = DcxFile.Write(DcxHeader.Emevd, Archive());
        var bnd = LuaBnd.Read(dcx);
        bnd.Add("555500_logic.lua", [9]);

        var reread = LuaBnd.Read(bnd.Write([7, 7], [8]));

        Assert.NotNull(reread.Dcx);
        Assert.Equal([7, 7], reread.GnlData);
        Assert.Equal([8], reread.InfoData);
        Assert.Contains(@"INTERROOT_x64", reread.Entries[1].Name, StringComparison.Ordinal);
    }
}
