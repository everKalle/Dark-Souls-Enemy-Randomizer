using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Infrastructure.Game;
using EnemyRandomizer.Infrastructure.Randomization;
using EnemyRandomizer.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace EnemyRandomizer.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers game detection, file stores and the randomizer service.</summary>
    public static IServiceCollection AddRandomizerInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IGamePaths, GamePaths>();
        services.AddSingleton<IGameInstallation, GameInstallation>();
        services.AddSingleton<IEnemyConfigRepository, EnemyConfigRepository>();
        services.AddSingleton<ILastConfigStore, JsonLastConfigStore>();
        services.AddSingleton<IRandomizerService, PreviewRandomizerService>();
        return services;
    }
}
