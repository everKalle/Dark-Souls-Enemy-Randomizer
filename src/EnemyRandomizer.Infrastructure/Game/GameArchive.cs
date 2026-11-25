using EnemyRandomizer.Formats.Bnd;
using EnemyRandomizer.Formats.Dcx;

namespace EnemyRandomizer.Infrastructure.Game;

/// <summary>A BND3 archive file, transparently DCX-compressed on Remastered (decorator over <see cref="Bnd3"/>).</summary>
public sealed class GameArchive
{
    private GameArchive(List<BndEntry> entries, DcxHeader? dcx)
    {
        Entries = entries;
        Dcx = dcx;
    }

    public List<BndEntry> Entries { get; }

    /// <summary>Header of the DCX container, <c>null</c> for an uncompressed archive.</summary>
    public DcxHeader? Dcx { get; }

    public static GameArchive Read(string file)
    {
        var bytes = File.ReadAllBytes(file);
        if (DcxFile.IsDcx(bytes))
        {
            var (header, content) = DcxFile.Read(bytes);
            return new GameArchive(Bnd3.Read(content), header);
        }

        return new GameArchive(Bnd3.Read(bytes), null);
    }

    /// <summary>Archive bytes, compressed again with the original DCX header when it was compressed.</summary>
    public byte[] ToBytes(IReadOnlyList<BndEntry>? entries = null)
    {
        var content = Bnd3.Write(entries ?? Entries);
        return Dcx is null ? content : DcxFile.Write(Dcx, content);
    }
}
