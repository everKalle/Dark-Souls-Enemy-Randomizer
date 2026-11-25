using EnemyRandomizer.Formats.Msb;

namespace EnemyRandomizer.Formats.Tests;

public class MsbLayoutTests
{
    [Fact]
    public void Creature_layout_matches_legacy_definition()
    {
        var layout = MsbLayout.Get("creatures2");

        Assert.Equal(25, layout.NameIndex);                 // 25 numeric fields before the name (0x64 bytes)
        Assert.True(layout.HasSib);
        Assert.Equal(100, Binary.StructFormat.CalcSize(layout.BeforeName));
        Assert.Equal("NPCParam", layout.Fields[layout.IndexOf("NPCParam")]);
        Assert.Equal(layout.IndexOf("PartIndex"), layout.PartIndex1);
    }

    [Fact]
    public void Event_layouts_include_the_template()
    {
        var layout = MsbLayout.Get("events5");

        Assert.Equal(8, layout.NameIndex);
        Assert.False(layout.HasSib);
        Assert.NotEqual(-1, layout.PartIndex1);
        Assert.NotEqual(-1, layout.PartIndex3);
        Assert.NotEqual(-1, layout.PointIndex2);
    }

    [Fact]
    public void Unknown_layout_is_rejected() =>
        Assert.Throws<InvalidDataException>(() => MsbLayout.Get("nope"));
}

public class MsbFileTests
{
    /// <summary>A small map: 1 model, 1 event, 2 points (unsorted), 1 map piece and 2 creatures.</summary>
    internal static MsbFile Sample()
    {
        var msb = new MsbFile();
        msb.Models.Add(32, 2, 0, 38, 1, 0, 0, 0, "c2500", @"N:\FRPG\data\Model\chr\c2500\sib\c2500.sib");

        // Event type is read from offset 0x8 (field 2), so it must match the table (0).
        var ev = msb.Events[0];
        ev.Add(Enumerable.Range(0, ev.Layout.Fields.Count)
                         .Select(i => i switch { 0 => 32, 2 => 0, _ when i == ev.Layout.NameIndex => "event", _ => (object)i })
                         .ToArray());

        foreach (var index in new[] { 1, 0 })
        {
            var points = msb.Points[0];
            var values = Zeros(points.Layout);
            values[0] = 56;
            values[2] = index;
            values[4] = 1.5f;
            values[points.Layout.NameIndex] = "point" + index;
            points.Add(values);
        }

        msb.Parts[0].Rows.Add(Part("mapPieces0", "m0000B0", 0));
        msb.Creatures.Rows.Add(Part("creatures2", "c2500_0000", 0));
        msb.Creatures.Rows.Add(Part("creatures2", "c2500_0001", 1));
        return msb;
    }

    /// <summary>Default values with the right type per field (int, float, or string).</summary>
    internal static object[] Zeros(MsbLayout layout)
    {
        var codes = Binary.StructFormat.Codes(layout.BeforeName)
                    + new string('s', layout.AfterNameIndex - layout.NameIndex)
                    + Binary.StructFormat.Codes(layout.AfterName);
        return codes.Select(c => c switch { 'f' => (object)0f, 's' => "", _ => 0 }).ToArray();
    }

    internal static MsbRow Part(string layoutName, string name, int partIndex)
    {
        var layout = MsbLayout.Get(layoutName);
        var values = Zeros(layout);
        const string sib = @"N:\FRPG\data\Model\sib\x.sib";
        values[0] = 0x64;                                    // name offset = size of the fields before the name
        values[1] = int.Parse(new string(layoutName.Where(char.IsAsciiDigit).ToArray()), System.Globalization.CultureInfo.InvariantCulture); // part type
        values[4] = 0x64 + name.Length + 1;                  // sib offset = right after the name
        values[5] = 1.25f;
        values[layout.NameIndex] = name;
        values[layout.NameIndex + 1] = sib;
        if (layout.PartIndex1 != -1)
        {
            values[layout.PartIndex1] = partIndex;
        }

        if (layout.IndexOf("NPCParam") is var npc and >= 0)
        {
            values[npc] = 250000;
        }

        return new MsbRow(layout, values);
    }

    [Fact]
    public void Write_then_read_keeps_every_value()
    {
        var msb = Sample();

        var read = MsbFile.Read(msb.Write());

        Assert.Equal(msb.Models.Rows.Single().Values, read.Models.Rows.Single().Values);
        Assert.Equal(msb.Events[0].Rows.Single().Values, read.Events[0].Rows.Single().Values);
        Assert.Equal(msb.Creatures.Rows.Select(r => r.Values), read.Creatures.Rows.Select(r => r.Values));
        Assert.Equal(250000, read.Creatures.Rows[1].GetInt("NPCParam"));
        Assert.Equal(1.25f, read.Creatures.Rows[1].GetFloat("X pos"));
    }

    [Fact]
    public void Writing_is_stable()
    {
        var bytes = Sample().Write();
        Assert.Equal(bytes, MsbFile.Read(bytes).Write());
    }

    [Fact]
    public void Points_are_written_sorted_by_index()
    {
        var read = MsbFile.Read(Sample().Write());
        Assert.Equal(["point0", "point1"], read.Points[0].Rows.Select(r => r.Name));
    }

    [Fact]
    public void Adding_a_creature_shifts_later_part_references()
    {
        var msb = Sample();
        var objectRow = Part("objects1", "o0000", 3);           // references part 3
        msb.Parts[1].Rows.Add(objectRow);
        var newPart = msb.Creatures.Rows[0].Clone();
        newPart.Name = "c2500_0002";

        msb.AddCreature(newPart);

        // new part index = 3 creatures + 1 object + 1 map piece - 1 = 4: references >= 4 move, 3 stays
        Assert.Equal(3, objectRow.GetInt("PartIndex"));
        Assert.Equal(3, msb.Creatures.Rows.Count);
    }

    [Fact]
    public void Named_field_access_reports_unknown_fields()
    {
        var row = Sample().Creatures.Rows[0];
        Assert.Throws<KeyNotFoundException>(() => row["NoSuchField"]);
    }
}
