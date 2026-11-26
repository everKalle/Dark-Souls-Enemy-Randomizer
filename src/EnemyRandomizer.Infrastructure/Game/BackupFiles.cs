namespace EnemyRandomizer.Infrastructure.Game;

/// <summary>
/// The <c>.bak</c> convention of v0.4.x: before a game file is changed the first time, the original is kept as
/// <c>&lt;file&gt;.bak</c>; "Revert to normal" copies it back (legacy <c>createBackup</c>/<c>restoreBackup</c>).
/// </summary>
public static class BackupFiles
{
    public const string Extension = ".bak";

    public static string BackupOf(string file) => file + Extension;

    /// <summary>Creates the backup once; never overwrites an existing one (it holds the original).</summary>
    public static void EnsureBackup(string file)
    {
        var backup = BackupOf(file);
        if (!File.Exists(backup) && File.Exists(file))
        {
            File.Copy(file, backup);
        }
    }

    /// <summary>Writes <paramref name="data"/> after making sure the original is backed up.</summary>
    public static void WriteWithBackup(string file, byte[] data)
    {
        EnsureBackup(file);
        AtomicFile.WriteAllBytes(file, data);
    }

    /// <summary>Copies the backup over the file. Returns false when there is no backup.</summary>
    public static bool Restore(string file)
    {
        var backup = BackupOf(file);
        if (!File.Exists(backup))
        {
            return false;
        }

        File.Copy(backup, file, overwrite: true);
        return true;
    }

    /// <summary>The unmodified version of a game file: its backup when one exists, else the file itself.</summary>
    public static string Original(string file) => File.Exists(BackupOf(file)) ? BackupOf(file) : file;

    /// <summary>A copy counts as broken below 10 bytes (legacy check for interrupted copies).</summary>
    public static bool IsValidCopy(string file) => File.Exists(file) && new FileInfo(file).Length >= 10;
}
