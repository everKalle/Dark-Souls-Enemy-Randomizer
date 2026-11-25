using System.Text.RegularExpressions;
using EnemyRandomizer.Formats.Bnd;
using EnemyRandomizer.Formats.Dcx;

namespace EnemyRandomizer.Formats.Lua;

/// <summary>
/// A map's AI script archive (<c>script/mXX_XX_XX_XX.luabnd[.dcx]</c>): compiled .lua files plus the
/// <c>.luagnl</c> (id 1000000) and <c>.luainfo</c> (id 1000001) entries. Legacy <c>bnd_data.py</c>.
/// </summary>
public sealed partial class LuaBnd
{
    public const int GnlId = 1000000;
    public const int InfoId = 1000001;

    private const string PtdeScriptRoot = @"N:\FRPG\data\INTERROOT_win32\script\ai\out\bin\";
    private const string RemasteredScriptRoot = @"N:\FRPG\data\INTERROOT_x64\script\ai\out\bin\";

    private LuaBnd(List<BndEntry> entries, DcxHeader? dcx)
    {
        Entries = entries;
        Dcx = dcx;
        MaxScriptId = entries.Where(e => e.Id is not GnlId and not InfoId).Select(e => e.Id).DefaultIfEmpty(0).Max();
    }

    public List<BndEntry> Entries { get; }

    /// <summary>Header of the DCX container (Remastered), <c>null</c> for plain PTDE files.</summary>
    public DcxHeader? Dcx { get; }

    public int MaxScriptId { get; private set; }

    public byte[] GnlData => Find(GnlId);

    public byte[] InfoData => Find(InfoId);

    /// <summary>Reads a plain or DCX-compressed luabnd.</summary>
    public static LuaBnd Read(ReadOnlySpan<byte> data)
    {
        if (DcxFile.IsDcx(data))
        {
            var (header, content) = DcxFile.Read(data);
            return new LuaBnd(Bnd3.Read(content), header);
        }

        return new LuaBnd(Bnd3.Read(data), null);
    }

    /// <summary>Writes the archive with new gnl/info data, compressed again when it was read compressed.</summary>
    public byte[] Write(byte[] gnlData, byte[] infoData)
    {
        var content = Bnd3.Write(Entries.Select(e => e.Id switch
        {
            GnlId => e with { Data = gnlData },
            InfoId => e with { Data = infoData },
            _ => e,
        }).ToList());

        return Dcx is null ? content : DcxFile.Write(Dcx, content);
    }

    /// <summary>Adds a compiled .lua file before the gnl/info entries (legacy <c>add</c>, inserts at index -2).</summary>
    public void Add(string scriptName, byte[] data)
    {
        var root = Dcx is null ? PtdeScriptRoot : RemasteredScriptRoot;
        MaxScriptId++;
        Entries.Insert(Math.Max(0, Entries.Count - 2), new BndEntry(MaxScriptId, root + scriptName, data));
    }

    /// <summary>Returns the battle/logic scripts (<c>123456_battle.lua</c>) with their file names (legacy <c>generateAiScripts</c>).</summary>
    public IEnumerable<(string FileName, byte[] Data)> AiScripts() =>
        Entries.Select(e => (Match: AiScriptName().Match(e.Name), e.Data))
               .Where(x => x.Match.Success)
               .Select(x => (x.Match.Value, x.Data));

    private byte[] Find(int id) => Entries.FirstOrDefault(e => e.Id == id)?.Data ?? [];

    [GeneratedRegex(@"[1-9][0-9]{5}_(battle|logic)\.lua")]
    private static partial Regex AiScriptName();
}
