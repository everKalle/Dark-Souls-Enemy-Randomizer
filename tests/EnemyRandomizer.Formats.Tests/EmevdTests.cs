using EnemyRandomizer.Formats.Dcx;
using EnemyRandomizer.Formats.Emevd;

namespace EnemyRandomizer.Formats.Tests;

public class InstructionCatalogTests
{
    [Fact]
    public void Contains_all_legacy_instructions()
    {
        var catalog = InstructionCatalog.Default;

        Assert.Equal(246, catalog.Count);
        Assert.Equal((2000, 0), catalog.Lookup("init_event"));
        Assert.Equal("@iII", catalog.Format(2000, 0));
        Assert.Equal("init_event", catalog.Name(2000, 0));
    }

    [Fact]
    public void Unknown_name_is_reported() =>
        Assert.Throws<KeyNotFoundException>(() => InstructionCatalog.Default.Lookup("nope"));
}

public class EmevdTests
{
    private static byte[] Remastered(string map) =>
        DcxFile.Read(File.ReadAllBytes(Path.Combine(Samples.Root, "emevd", "REMASTER", map + ".emevd.dcx"))).Data;

    private static byte[] Ptde(string map) =>
        File.ReadAllBytes(Path.Combine(Samples.Root, "emevd", "PTDE", map + ".emevd"));

    /// <summary>Every event, instruction, argument and parameter as text, for content comparison.</summary>
    private static IEnumerable<string> Content(EmevdFile file) =>
        file.Events.SelectMany(e => e.Instructions.Select(i =>
            $"{e.Id}/{e.RestBehavior} [{i.Class},{i.Index}] {i.ArgumentFormat} ({string.Join(",", i.Args)}) " +
            string.Join(" ", i.Parameters.Select(p => $"{p.DestinationStartByte}<{p.SourceStartByte}:{p.Length}"))));

    /// <summary>
    /// Original game files carry 4 padding bytes after the argument block that the v0.4.x writer never wrote
    /// (the game accepts both). So: content must survive, and writing must be stable.
    /// </summary>
    [Theory]
    [MemberData(nameof(Samples.Maps), MemberType = typeof(Samples))]
    public void Read_then_write_keeps_all_content(string map)
    {
        foreach (var original in new[] { Ptde(map), Remastered(map) })
        {
            var read = EmevdFile.Read(original);
            var written = read.Write();

            Assert.Equal(Content(read), Content(EmevdFile.Read(written)));
            Assert.Equal(written, EmevdFile.Read(written).Write());
            Assert.InRange(original.Length - written.Length, 0, 4);
        }
    }

    /// <summary>
    /// The shipped dkscripts are the (lossy) text form of the shipped .emevd files: compiling them must give the
    /// same events with the same instruction counts, and the compiled file must survive its own read/write.
    /// </summary>
    [Theory]
    [MemberData(nameof(Samples.Maps), MemberType = typeof(Samples))]
    public void Shipped_dkscript_matches_shipped_emevd_structure(string map)
    {
        foreach (var (edition, shipped) in new[] { ("PTDE", Ptde(map)), ("Remastered", Remastered(map)) })
        {
            var expected = EmevdFile.Read(shipped);
            var compiled = DkScript.Compile(File.ReadAllText(Samples.DkScript(edition, map)));

            Assert.Equal(expected.Events.Select(e => (e.Id, e.RestBehavior, e.Instructions.Count)),
                         compiled.Events.Select(e => (e.Id, e.RestBehavior, e.Instructions.Count)));

            var bytes = compiled.Write();
            Assert.Equal(bytes, EmevdFile.Read(bytes).Write());
        }
    }

    [Theory]
    [MemberData(nameof(Samples.Maps), MemberType = typeof(Samples))]
    public void Export_then_compile_keeps_events_and_instructions(string map)
    {
        var original = EmevdFile.Read(Ptde(map));

        var compiled = DkScript.Compile(DkScript.Export(original));

        Assert.Equal(original.Events.Select(e => (e.Id, e.Instructions.Count)), compiled.Events.Select(e => (e.Id, e.Instructions.Count)));
        Assert.Equal(
            original.Events.SelectMany(e => e.Instructions).Where(i => i.Parameters.Count == 0).Select(i => (i.Class, i.Index, string.Join(",", i.Args))),
            compiled.Events.SelectMany(e => e.Instructions).Where(i => i.Parameters.Count == 0).Select(i => (i.Class, i.Index, string.Join(",", i.Args))));
    }

    [Fact]
    public void Init_event_keeps_arguments_the_started_event_does_not_declare()
    {
        // m13_00 event 11305100 reads only offset 4 of its arguments; v0.4.x dropped the last 4 bytes.
        var file = EmevdFile.Read(Ptde("m13_00_00_00"));
        var call = file.Events.SelectMany(e => e.Instructions)
                       .First(i => i.IsInitEvent && Convert.ToUInt32(i.Args[1], System.Globalization.CultureInfo.InvariantCulture) == 11305100);

        Assert.Equal("@iIiI", call.ArgumentFormat);
        Assert.Equal(4, call.Args.Count);
    }

    [Fact]
    public void Event_templates_compile()
    {
        var file = DkScript.Compile(File.ReadAllText(Samples.EventTemplates));

        Assert.Equal(
            [10009000u, 10009010u, 10008800u, 11015396u, 11015382u, 11205382u, 11810310u],
            file.Events.Select(e => e.Id));
        Assert.All(file.Events, e => Assert.NotEmpty(e.Instructions));
    }

    [Fact]
    public void Clone_is_deep()
    {
        var template = DkScript.Compile(File.ReadAllText(Samples.EventTemplates)).Events[0];

        var copy = template.Clone();
        copy.Id = 1;
        copy.Instructions[0].Args[0] = 12345;

        Assert.NotEqual(1u, template.Id);
        Assert.NotEqual(12345, template.Instructions[0].Args[0]);
    }

    [Fact]
    public void New_init_event_instruction_writes_and_reads_back()
    {
        var file = EmevdFile.Read(Ptde("m13_00_00_00"));
        file.Events[0].Instructions.Add(EmevdInstruction.Create(2000, 0, "@iII", 0, 11009000u, 1234u));

        var reread = EmevdFile.Read(file.Write());

        var added = reread.Events[0].Instructions[^1];
        Assert.True(added.IsInitEvent);
        Assert.Equal(11009000u, Convert.ToUInt32(added.Args[1], System.Globalization.CultureInfo.InvariantCulture));
    }
}
