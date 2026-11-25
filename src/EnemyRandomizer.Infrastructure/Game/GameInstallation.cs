using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Game;

/// <summary>
/// Detects the edition by which exe exists (PTDE is checked first, like <c>check_exe.py:31-52</c>).
/// Hash based exe states follow in migration step 3.
/// </summary>
public sealed class GameInstallation : IGameInstallation
{
    private readonly Lazy<GameEdition?> _edition;

    public GameInstallation(IGamePaths paths, IOptions<GameDataSettings> gameData)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(gameData);
        _edition = new Lazy<GameEdition?>(() => Detect(paths.GameDirectory, gameData.Value));
    }

    public GameEdition? Edition => _edition.Value;

    internal static GameEdition? Detect(string gameDirectory, GameDataSettings gameData) =>
        new[] { GameEdition.Ptde, GameEdition.Remastered }
            .Where(e => gameData.Editions.ContainsKey(e))
            .Select(e => (GameEdition?)e)
            .FirstOrDefault(e => File.Exists(Path.Combine(gameDirectory, gameData.Editions[e!.Value].ExeName)));
}
