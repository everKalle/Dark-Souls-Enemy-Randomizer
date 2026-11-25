using System.Security.Cryptography;
using EnemyRandomizer.Core;
using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Core.Installation;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Game;

/// <summary>
/// Detects edition and exe state (legacy <c>check_exe.py:17-52</c>): a PTDE exe is identified by its SHA-256,
/// unknown hashes are <see cref="ExeState.Unknown"/>; otherwise the Remastered exe is looked for.
/// </summary>
public sealed class GameInstallation(IGamePaths paths, IOptions<GameDataSettings> gameData) : IGameInstallation
{
    private ExeState? _state;

    public ExeState ExeState => _state ??= Detect(paths.GameDirectory, gameData.Value);

    public GameEdition? Edition => ExeStates.EditionOf(ExeState);

    public void Refresh() => _state = null;

    internal static ExeState Detect(string gameDirectory, GameDataSettings data)
    {
        var ptde = data.Editions[GameEdition.Ptde];
        var ptdeExe = Path.Combine(gameDirectory, ptde.ExeName);
        if (File.Exists(ptdeExe))
        {
            var hash = Sha256(ptdeExe);
            return ptde.ExeHashes.FirstOrDefault(h => string.Equals(h.Key, hash, StringComparison.OrdinalIgnoreCase)) is { Key: not null } match
                ? match.Value
                : ExeState.Unknown;
        }

        return File.Exists(Path.Combine(gameDirectory, data.Editions[GameEdition.Remastered].ExeName))
            ? ExeState.Remastered
            : ExeState.NotFound;
    }

    internal static string Sha256(string file)
    {
        using var stream = File.OpenRead(file);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
}
