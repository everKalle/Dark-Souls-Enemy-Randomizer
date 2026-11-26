using System.Buffers.Binary;
using EnemyRandomizer.Formats.Bnd;
using EnemyRandomizer.Formats.Dcx;
using EnemyRandomizer.Formats.Emevd;
using EnemyRandomizer.Formats.Msb;

namespace EnemyRandomizer.Formats.Tests;

/// <summary>Broken or hostile files must end as <see cref="InvalidDataException"/>, never as an unrelated crash or a huge allocation.</summary>
public class MalformedInputTests
{
    [Fact]
    public void Bnd3_with_a_truncated_header_is_rejected() =>
        Assert.Throws<InvalidDataException>(() => Bnd3.Read("BND3"u8.ToArray()));

    [Fact]
    public void Bnd3_with_an_impossible_entry_count_is_rejected()
    {
        var data = Bnd3.Write([new BndEntry(1, "a", [1, 2, 3])]);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x10), int.MaxValue);

        Assert.Throws<InvalidDataException>(() => Bnd3.Read(data));
    }

    [Fact]
    public void Bnd3_with_an_entry_outside_the_file_is_rejected()
    {
        var data = Bnd3.Write([new BndEntry(1, "a", [1, 2, 3])]);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x20 + 4), 1_000_000);  // size of the first record

        Assert.Throws<InvalidDataException>(() => Bnd3.Read(data));
    }

    [Fact]
    public void Dcx_with_a_decompression_bomb_size_is_rejected()
    {
        var data = DcxFile.Write(DcxHeader.Emevd, new byte[64]);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(28), uint.MaxValue);

        Assert.Throws<InvalidDataException>(() => DcxFile.Read(data));
    }

    [Fact]
    public void Dcx_with_content_shorter_than_announced_is_rejected()
    {
        var data = DcxFile.Write(DcxHeader.Emevd, new byte[64]);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(28), 4096);

        Assert.Throws<InvalidDataException>(() => DcxFile.Read(data));
    }

    [Fact]
    public void Dcx_with_a_truncated_header_is_rejected() =>
        Assert.Throws<InvalidDataException>(() => DcxFile.Read("DCX\0"u8.ToArray()));

    [Fact]
    public void Msb_with_garbage_offsets_is_rejected() =>
        Assert.Throws<InvalidDataException>(() => MsbFile.Read(Enumerable.Repeat((byte)0x7F, 256).ToArray()));

    [Fact]
    public void Emevd_with_garbage_offsets_is_rejected()
    {
        var data = new byte[256];
        "EVD\0"u8.CopyTo(data);
        data[8] = 0xCC;
        data.AsSpan(0x0C).Fill(0x7F);

        Assert.Throws<InvalidDataException>(() => EmevdFile.Read(data));
    }
}
