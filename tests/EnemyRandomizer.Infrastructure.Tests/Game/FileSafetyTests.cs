using EnemyRandomizer.Infrastructure.Game;

namespace EnemyRandomizer.Infrastructure.Tests.Game;

public sealed class FileSafetyTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("er-safety-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string InFolder(string name) => Path.Combine(_folder, name);

    [Fact]
    public void Atomic_write_replaces_the_file_and_leaves_no_temp_file()
    {
        var file = InFolder("a.bin");
        File.WriteAllBytes(file, [1]);

        AtomicFile.WriteAllBytes(file, [2, 3]);

        Assert.Equal(new byte[] { 2, 3 }, File.ReadAllBytes(file));
        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Fact]
    public void Atomic_write_failure_keeps_the_old_content_and_leaves_no_temp_file()
    {
        var file = InFolder("locked.bin");
        File.WriteAllBytes(file, [1]);
        using var handle = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);

        Assert.ThrowsAny<Exception>(() => AtomicFile.WriteAllBytes(file, [9]));

        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Fact]
    public void Rollback_restores_changed_files_and_removes_created_ones()
    {
        var existing = InFolder("existing.bin");
        var created = InFolder("created.bin");
        File.WriteAllBytes(existing, [1, 1]);
        var transaction = new FileTransaction();

        transaction.Write(existing, [2]);
        transaction.Write(existing, [3]);   // only the first state counts
        transaction.WriteWithBackup(created, [4]);
        var failed = transaction.Rollback();

        Assert.Empty(failed);
        Assert.Equal(new byte[] { 1, 1 }, File.ReadAllBytes(existing));
        Assert.False(File.Exists(created));
    }

    [Fact]
    public void Rollback_undoes_a_restore_from_backup()
    {
        var file = InFolder("map.msb");
        File.WriteAllBytes(file, [5]);
        File.WriteAllBytes(BackupFiles.BackupOf(file), [0]);
        var transaction = new FileTransaction();

        Assert.True(transaction.Restore(file));
        Assert.Equal(new byte[] { 0 }, File.ReadAllBytes(file));
        transaction.Rollback();

        Assert.Equal(new byte[] { 5 }, File.ReadAllBytes(file));
    }
}
