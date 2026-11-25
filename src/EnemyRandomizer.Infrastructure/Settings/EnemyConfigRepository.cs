using System.Text;
using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.EnemyConfigs;
using EnemyRandomizer.Core.Settings;

namespace EnemyRandomizer.Infrastructure.Settings;

/// <summary>
/// Enemy configs as files: the built-in catalog <c>replacement_ref/valid_new.txt</c> ("Default")
/// and user configs in <c>customConfigs/&lt;name&gt;.txt</c>.
/// </summary>
public sealed class EnemyConfigRepository(IGamePaths paths, IGameInstallation installation) : IEnemyConfigRepository
{
    private const string Extension = ".txt";
    private const string CatalogFileName = "valid_new.txt";

    public event EventHandler? ConfigsChanged;

    private string CatalogFile => Path.Combine(paths.ReplacementReferenceDirectory, CatalogFileName);

    public IReadOnlyList<string> ListNames()
    {
        var names = new List<string> { RandomizerSettings.DefaultEnemyConfig };
        if (Directory.Exists(paths.CustomConfigDirectory))
        {
            names.AddRange(Directory.EnumerateFiles(paths.CustomConfigDirectory, "*" + Extension)
                                    .Select(Path.GetFileNameWithoutExtension)
                                    .OfType<string>()
                                    .Where(n => !IsDefault(n))
                                    .Order(StringComparer.OrdinalIgnoreCase));
        }

        return names;
    }

    public IReadOnlyList<EnemyConfigEntry> Load(string name) =>
        EnemyConfigFile.Parse(File.ReadAllLines(FilePath(name)), installation.Edition ?? GameEdition.Ptde);

    public bool Exists(string name) => IsDefault(name) || File.Exists(FilePath(name));

    public bool IsValidName(string name) =>
        !string.IsNullOrWhiteSpace(name)
        && name.Trim() == name
        && !IsDefault(name)
        && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    public string Save(string name, IReadOnlyList<bool> enabled)
    {
        if (!IsValidName(name))
        {
            throw new ArgumentException($"'{name}' is not a valid config name.", nameof(name));
        }

        var content = EnemyConfigFile.Write(File.ReadAllLines(CatalogFile), enabled);
        Directory.CreateDirectory(paths.CustomConfigDirectory);
        var file = FilePath(name);
        File.WriteAllText(file, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        ConfigsChanged?.Invoke(this, EventArgs.Empty);
        return file;
    }

    private string FilePath(string name) =>
        IsDefault(name) ? CatalogFile : Path.Combine(paths.CustomConfigDirectory, name + Extension);

    private static bool IsDefault(string name) =>
        string.Equals(name, RandomizerSettings.DefaultEnemyConfig, StringComparison.OrdinalIgnoreCase);
}
