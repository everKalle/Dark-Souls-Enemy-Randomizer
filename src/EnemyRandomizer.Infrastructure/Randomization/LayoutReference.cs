using System.Globalization;
using System.Text;
using EnemyRandomizer.Formats.Msb;
using EnemyRandomizer.Infrastructure.Game;

namespace EnemyRandomizer.Infrastructure.Randomization;

/// <summary>
/// Writer for the enemy layout reference file (<c>refs/enemy-layout-&lt;time&gt;.txt</c>, legacy <c>WriteReferenceRow</c>).
/// Same format as v0.4.x so files can be exchanged both ways:
/// <code>
/// section version / randomizerVersion X / mode PTDE|Remastered
/// section settings / bossSouls N / pinwheelChaos N / roamingBossRespawn N
/// section mXX_XX_XX_XX
/// name  x y z  rx ry rz  model ai param  entityId animId   (tab separated)
/// levID N
/// </code>
/// </summary>
public sealed class LayoutReferenceWriter
{
    private readonly StringBuilder _text = new();

    public void Header(string version, bool remastered, int bossSouls, int pinwheelChaos, int roamingBossRespawn)
    {
        Line("section version");
        Line($"randomizerVersion {version}");
        Line($"mode {(remastered ? "Remastered" : "PTDE")}");
        Line("section settings");
        Line($"bossSouls {bossSouls}");
        Line($"pinwheelChaos {pinwheelChaos}");
        Line($"roamingBossRespawn {roamingBossRespawn}");
    }

    public void BeginMap(string mapId) => Line($"section {mapId}");

    public void Row(MsbRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        string F(string field) => Convert.ToString(row[field], CultureInfo.InvariantCulture) ?? "";
        Line(string.Join('\t',
            row.Name, F("X pos"), F("Y pos"), F("Z pos"), F("Rot X"), F("Rot Y"), F("Rot Z"),
            F("Model"), F("AI"), F("NPCParam"), F("EventEntityID"), F("AnimID")));
    }

    public void EndMap(int nextEventEntityId) => Line($"levID {nextEventEntityId}");

    public void Save(string file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        AtomicFile.WriteAllText(file, _text.ToString());
    }

    private void Line(string text) => _text.Append(text).Append('\n');
}

/// <summary>Plain text run log (<c>logs/rlog&lt;time&gt;.txt</c>), like v0.4.x.</summary>
public sealed class RunLog
{
    private readonly StringBuilder _text = new();

    public void Line(string text) => _text.Append(text).Append('\n');

    public void Save(string file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        AtomicFile.WriteAllText(file, _text.ToString());
    }
}
