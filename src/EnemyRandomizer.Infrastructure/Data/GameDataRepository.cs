using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Data;
using EnemyRandomizer.Core.Settings;
using EnemyRandomizer.Infrastructure.Game;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Data;

/// <summary>
/// Reads the data files of <c>enemyRandomizerData</c>. Files that never change during a session
/// (replacement targets, AI references) are loaded once on first use (lazy proxy); enemy configs and map
/// references are read on every call because the user can edit them while the app runs.
/// </summary>
public sealed partial class GameDataRepository : IGameDataRepository
{
    private const string AiReferenceFile = "airef.csv";
    private const string ReplacementTargetFile = "valid_replacements.txt";
    private const string CatalogFile = "valid_new.txt";

    private readonly IGamePaths _paths;
    private readonly GameFileLayout _layout;
    private readonly int _maxDifficulty;
    private readonly ILogger<GameDataRepository> _logger;
    private readonly Lazy<ReplacementTargets> _targets;
    private readonly Lazy<IReadOnlyDictionary<int, AiReference>> _aiReferences;

    public GameDataRepository(
        IGamePaths paths,
        GameFileLayout layout,
        IOptions<DifficultyCurveSettings> difficulty,
        ILogger<GameDataRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(difficulty);
        _paths = paths;
        _layout = layout;
        _maxDifficulty = difficulty.Value.MaxClass;
        _logger = logger;
        _targets = new(() => new ReplacementTargets(DataFiles.ParseReplacementTargets(
            File.ReadLines(Path.Combine(_paths.ReplacementReferenceDirectory, ReplacementTargetFile)))));
        _aiReferences = new(LoadAiReferences);
    }

    public ReplacementTargets ReplacementTargets => _targets.Value;

    public IReadOnlyDictionary<int, AiReference> AiReferences => _aiReferences.Value;

    public IReadOnlyList<EnemyDefinition> LoadCatalog(string enemyConfig)
    {
        var file = string.Equals(enemyConfig, RandomizerSettings.DefaultEnemyConfig, StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(_paths.ReplacementReferenceDirectory, CatalogFile)
            : Path.Combine(_paths.CustomConfigDirectory, enemyConfig + ".txt");

        var catalog = DataFiles.ParseCatalog(File.ReadLines(file), _maxDifficulty, out var skipped);
        foreach (var id in skipped)
        {
            LogSkippedRow(_logger, id, enemyConfig);
        }

        return catalog;
    }

    public EnemyPool LoadPool(string enemyConfig, GameEdition edition, bool hostileNpcs) =>
        EnemyPool.Build(LoadCatalog(enemyConfig), edition, hostileNpcs, _maxDifficulty);

    public IReadOnlyList<OriginalEnemy> LoadOriginalEnemies(string mapId, GameEdition edition)
    {
        var map = _layout.Maps.FirstOrDefault(m => m.Id == mapId)
                  ?? throw new ArgumentException($"Unknown map '{mapId}'.", nameof(mapId));
        var file = Path.Combine(_paths.OriginalEnemyReferenceDirectory, GameFileLayout.OriginalReferenceFile(map, edition));
        return DataFiles.ParseOriginalEnemies(File.ReadLines(file));
    }

    private Dictionary<int, AiReference> LoadAiReferences()
    {
        var result = new Dictionary<int, AiReference>();
        foreach (var reference in DataFiles.ParseAiReferences(File.ReadLines(_paths.InData(AiReferenceFile))))
        {
            result.TryAdd(reference.NpcAi, reference); // first row wins, like GetEntryByAI
        }

        return result;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Enemy {Id} skipped in config {Config}: AI and param lists differ in length")]
    private static partial void LogSkippedRow(ILogger logger, string id, string config);
}
