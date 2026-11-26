using System.Text;

namespace EnemyRandomizer.Infrastructure.Game;

/// <summary>
/// Crash-safe file writes: the content goes to a temporary file next to the target first and is then moved over it,
/// so an interrupted write never leaves a truncated target behind.
/// </summary>
public static class AtomicFile
{
    public static void WriteAllBytes(string file, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var temp = file + ".tmp";
        try
        {
            File.WriteAllBytes(temp, data);
            File.Move(temp, file, overwrite: true);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    public static void WriteAllText(string file, string text, Encoding? encoding = null) =>
        WriteAllBytes(file, (encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)).GetBytes(text));

    private static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort only; the original failure is the one worth reporting.
        }
    }
}
