namespace EnemyRandomizer.Formats.Tests;

/// <summary>Paths of the reference files copied next to the test assembly.</summary>
internal static class Samples
{
    public static string Root => Path.Combine(AppContext.BaseDirectory, "samples");

    public static IEnumerable<string> PtdeEmevd => Directory.EnumerateFiles(Path.Combine(Root, "emevd", "PTDE"), "*.emevd");

    public static IEnumerable<string> RemasteredEmevdDcx => Directory.EnumerateFiles(Path.Combine(Root, "emevd", "REMASTER"), "*.emevd.dcx");

    public static string DkScript(string edition, string map) => Path.Combine(Root, "dkscript", edition, map + ".dkscript");

    public static string EventTemplates => Path.Combine(Root, "eventTemplates.dkscript");

    public static TheoryData<string> Maps => new(["m12_01_00_00", "m13_00_00_00", "m14_01_00_00", "m15_00_00_00", "m15_01_00_00", "m17_00_00_00"]);
}
