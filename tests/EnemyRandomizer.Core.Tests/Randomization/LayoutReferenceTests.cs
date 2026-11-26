using EnemyRandomizer.Core.Randomization;

namespace EnemyRandomizer.Core.Tests.Randomization;

public sealed class LayoutReferenceTests
{
    private const string Header =
        "section version\nrandomizerVersion 0.4.1.2\nmode PTDE\nsection settings\nbossSouls 50\npinwheelChaos 0\nroamingBossRespawn 1\n";

    private const string Map =
        "section m10_00_00_00\nc2510_0000\t1.5\t2\t-3.25\t0\t90\t0\t123\t4000\t5000\t-1\t7\nlevID 1234\n";

    [Fact]
    public void Parses_a_file_written_by_the_legacy_tool()
    {
        var reference = LayoutReference.Parse((Header + Map).Split('\n'));

        Assert.Equal("0.4.1.2", reference.RandomizerVersion);
        Assert.False(reference.Remastered);
        Assert.Equal((50, 0, 1), (reference.BossSouls, reference.PinwheelChaos, reference.RoamingBossRespawn));
        var map = reference.Maps["m10_00_00_00"];
        Assert.Equal(1234, map.NextEventEntityId);
        Assert.Equal(new LayoutRow(1.5f, 2, -3.25f, 0, 90, 0, 123, 4000, 5000, -1, 7), map.Rows["c2510_0000"]);
    }

    [Theory]
    [InlineData("section settings\nbossSouls 1\npinwheelChaos 0\nroamingBossRespawn 0\n", "version")]
    [InlineData("section version\nrandomizerVersion 1\nmode PTDE\n", "bossSouls")]
    [InlineData("section version\nrandomizerVersion 1\nmode Xbox\nsection settings\nbossSouls 1\npinwheelChaos 0\nroamingBossRespawn 0\n", "Invalid randomizer mode")]
    public void Rejects_missing_or_invalid_header_information(string text, string expectedMessagePart)
    {
        var error = Assert.Throws<InvalidDataException>(() => LayoutReference.Parse(text.Split('\n')));
        Assert.Contains(expectedMessagePart, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("c2510_0000\t1\t2\t3")]
    [InlineData("c2510_0000\tx\t2\t3\t4\t5\t6\t7\t8\t9\t10\t11")]
    public void Rejects_malformed_creature_lines(string row)
    {
        var text = Header + "section m10_00_00_00\n" + row + "\n";
        var error = Assert.Throws<InvalidDataException>(() => LayoutReference.Parse(text.Split('\n')));
        Assert.Contains("Malformed line", error.Message, StringComparison.Ordinal);
    }
}
