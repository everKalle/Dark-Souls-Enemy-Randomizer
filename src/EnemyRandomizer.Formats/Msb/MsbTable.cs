using System.Globalization;

namespace EnemyRandomizer.Formats.Msb;

/// <summary>
/// One MSB row. <see cref="Values"/> follows <see cref="MsbLayout.Fields"/>: numbers (<see cref="int"/>/<see cref="float"/>)
/// and the name / sib path as <see cref="string"/>.
/// </summary>
public sealed class MsbRow(MsbLayout layout, object[] values)
{
    public MsbLayout Layout { get; } = layout;

    public object[] Values { get; } = values;

    public string Name
    {
        get => (string)Values[Layout.NameIndex];
        set => Values[Layout.NameIndex] = value;
    }

    public object this[int index]
    {
        get => Values[index];
        set => Values[index] = value;
    }

    /// <summary>Value of a named field (e.g. <c>"NPCParam"</c>, <c>"X pos"</c>).</summary>
    public object this[string field]
    {
        get => Values[IndexOf(field)];
        set => Values[IndexOf(field)] = value;
    }

    public int GetInt(string field) => Convert.ToInt32(this[field], CultureInfo.InvariantCulture);

    public float GetFloat(string field) => Convert.ToSingle(this[field], CultureInfo.InvariantCulture);

    public MsbRow Clone() => new(Layout, (object[])Values.Clone());

    private int IndexOf(string field)
    {
        var index = Layout.IndexOf(field);
        return index >= 0 ? index : throw new KeyNotFoundException($"Layout '{Layout.Name}' has no field '{field}'.");
    }
}

/// <summary>All rows of one layout (legacy <c>Msbdata.rows</c>).</summary>
public sealed class MsbTable(string layoutName)
{
    public MsbLayout Layout { get; } = MsbLayout.Get(layoutName);

    public List<MsbRow> Rows { get; } = [];

    /// <summary>Adds a row from values in field order.</summary>
    public MsbRow Add(params object[] values)
    {
        if (values.Length != Layout.Fields.Count)
        {
            throw new ArgumentException($"Layout '{Layout.Name}' has {Layout.Fields.Count} fields, got {values.Length}.", nameof(values));
        }

        var row = new MsbRow(Layout, values);
        Rows.Add(row);
        return row;
    }

    /// <summary>Shifts part references after a part was inserted at <paramref name="newPartIndex"/>.</summary>
    internal void UpdatePartIndices(int newPartIndex)
    {
        foreach (var row in Rows)
        {
            foreach (var index in new[] { Layout.PartIndex1, Layout.PartIndex2, Layout.PartIndex3 })
            {
                if (index != -1 && Convert.ToInt32(row[index], CultureInfo.InvariantCulture) >= newPartIndex)
                {
                    row[index] = Convert.ToInt32(row[index], CultureInfo.InvariantCulture) + 1;
                }
            }
        }
    }
}
