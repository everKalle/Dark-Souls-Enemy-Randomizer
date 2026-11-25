using EnemyRandomizer.Formats.Binary;
using EnemyRandomizer.Formats.Bnd;
using EnemyRandomizer.Formats.Dcx;

namespace EnemyRandomizer.Formats.Tests;

public class StructFormatTests
{
    // Expected sizes follow CPython's struct.calcsize on x86/x64 (native alignment, no trailing padding).
    [Theory]
    [InlineData("@", 0)]
    [InlineData("@iB", 5)]
    [InlineData("@Bi", 8)]
    [InlineData("@bihh", 12)]
    [InlineData("@ihhif", 16)]
    [InlineData("@biifhfiBi", 36)]
    [InlineData("@iIBBBB", 12)]
    [InlineData("@hh", 4)]
    [InlineData("<IHih", 12)]
    [InlineData("<IIIIIIIIIIIIIIIIII", 72)]
    [InlineData("iB", 5)]
    public void CalcSize_matches_python(string format, int expected) =>
        Assert.Equal(expected, StructFormat.CalcSize(format));

    [Fact]
    public void Pack_inserts_alignment_padding()
    {
        var bytes = StructFormat.Pack("@Bi", [7, -2]);
        Assert.Equal(new byte[] { 7, 0, 0, 0, 0xFE, 0xFF, 0xFF, 0xFF }, bytes);
    }

    [Fact]
    public void Pack_and_unpack_round_trip()
    {
        object[] values = [-5, 200, 1, -300, 60000, -70000, 4000000000u, 1.5f];
        var bytes = StructFormat.Pack("@bB?hHiIf", values);

        var result = StructFormat.Unpack("@bB?hHiIf", bytes);

        Assert.Equal([-5, 200, true, -300, 60000, -70000, 4000000000u, 1.5f], result);
    }

    [Fact]
    public void Big_endian_is_supported() =>
        Assert.Equal(new byte[] { 0, 0, 1, 0 }, StructFormat.Pack(">I", [256]));

    [Fact]
    public void Out_of_range_value_fails_like_python() =>
        Assert.Throws<OverflowException>(() => StructFormat.Pack("@B", [256]));
}

public class Bnd3Tests
{
    private static List<BndEntry> Sample() =>
    [
        new(1, @"N:\FRPG\data\a.lua", [1, 2, 3]),
        new(1000000, @"N:\FRPG\data\m10.luagnl", [4, 5, 6, 7, 8]),
        new(1000001, @"N:\FRPG\data\m10.luainfo", []),
    ];

    [Fact]
    public void Write_then_read_round_trips()
    {
        var entries = Sample();

        var read = Bnd3.Read(Bnd3.Write(entries));

        Assert.Equal(entries.Select(e => (e.Id, e.Name)), read.Select(e => (e.Id, e.Name)));
        Assert.Equal(entries.Select(e => e.Data), read.Select(e => e.Data));
    }

    [Fact]
    public void Data_is_aligned_to_16_bytes()
    {
        var bytes = Bnd3.Write(Sample());

        Assert.True(bytes.AsSpan().StartsWith("BND307D7R6"u8));
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(0, BitConverter.ToInt32(bytes, 0x20 + (i * 24) + 8) % 16);
        }
    }

    [Fact]
    public void Shift_jis_names_survive()
    {
        var read = Bnd3.Read(Bnd3.Write([new BndEntry(5, "テスト.lua", [9])]));
        Assert.Equal("テスト.lua", read[0].Name);
    }

    [Fact]
    public void Unknown_flag_is_rejected()
    {
        var bytes = Bnd3.Write(Sample());
        bytes[0x0C] = 0x99;
        Assert.Throws<InvalidDataException>(() => Bnd3.Read(bytes));
    }
}

public class DcxTests
{
    [Fact]
    public void Shipped_remastered_events_decompress()
    {
        foreach (var file in Samples.RemasteredEmevdDcx)
        {
            var (header, data) = DcxFile.Read(File.ReadAllBytes(file));

            Assert.True(data.AsSpan().StartsWith("EVD\0"u8), file);
            Assert.Equal(DcxHeader.Emevd.UnknownHeaderPart, header.UnknownHeaderPart);
            Assert.Equal(DcxHeader.Emevd.HeaderLength, header.HeaderLength);
            Assert.Equal(DcxHeader.Emevd.CompressedHeaderLength, header.CompressedHeaderLength);
        }
    }

    [Fact]
    public void Write_then_read_round_trips()
    {
        var content = Enumerable.Range(0, 5000).Select(i => (byte)(i % 7)).ToArray();

        var (header, data) = DcxFile.Read(DcxFile.Write(DcxHeader.Emevd, content));

        Assert.Equal(content, data);
        Assert.Equal(DcxHeader.Emevd.Req1, header.Req1);
    }

    [Fact]
    public void Garbage_is_rejected() =>
        Assert.Throws<InvalidDataException>(() => DcxFile.Read(new byte[100]));
}
