namespace EnemyRandomizer.Infrastructure.Game;

/// <summary>
/// Remembers the content of files before they are changed so a failed or cancelled run can put the game back into the
/// state it had before the run. Every file is captured once, on its first change.
/// </summary>
public sealed class FileTransaction
{
    private readonly Dictionary<string, byte[]?> _snapshots = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Captures <paramref name="file"/> (or its absence); a no-op when it was captured before.</summary>
    public void Track(string file)
    {
        if (!_snapshots.ContainsKey(file))
        {
            _snapshots[file] = File.Exists(file) ? File.ReadAllBytes(file) : null;
        }
    }

    /// <summary>Tracked version of <see cref="BackupFiles.WriteWithBackup"/>.</summary>
    public void WriteWithBackup(string file, byte[] data)
    {
        Track(file);
        BackupFiles.WriteWithBackup(file, data);
    }

    /// <summary>Tracked atomic write.</summary>
    public void Write(string file, byte[] data)
    {
        Track(file);
        AtomicFile.WriteAllBytes(file, data);
    }

    /// <summary>Tracked version of <see cref="BackupFiles.Restore"/>.</summary>
    public bool Restore(string file)
    {
        Track(file);
        return BackupFiles.Restore(file);
    }

    /// <summary>Tracked copy over <paramref name="target"/>.</summary>
    public void Copy(string source, string target)
    {
        Track(target);
        File.Copy(source, target, overwrite: true);
    }

    /// <summary>Puts every tracked file back. Returns the files that could not be restored.</summary>
    public IReadOnlyList<string> Rollback()
    {
        var failed = new List<string>();
        foreach (var (file, content) in _snapshots)
        {
            try
            {
                if (content is null)
                {
                    File.Delete(file);
                }
                else
                {
                    AtomicFile.WriteAllBytes(file, content);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failed.Add(file);
            }
        }

        _snapshots.Clear();
        return failed;
    }
}
