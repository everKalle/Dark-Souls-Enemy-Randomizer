using EnemyRandomizer.Core.Abstractions;
using EnemyRandomizer.Infrastructure.Game;
using EnemyRandomizer.Infrastructure.Installation;
using EnemyRandomizer.Infrastructure.Randomization;
using EnemyRandomizer.Infrastructure.Settings;
using EnemyRandomizer.Infrastructure.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace EnemyRandomizer.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers game detection, startup checks, file stores and the randomizer service.</summary>
    public static IServiceCollection AddRandomizerInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IGamePaths, GamePaths>();
        services.AddSingleton<GameFileLayout>();
        services.AddSingleton<IGameInstallation, GameInstallation>();

        // Startup checks, run in registration order (composite).
        services.AddSingleton<IInstallationCheck, ExeCheck>();
        services.AddSingleton<IInstallationCheck, GameFilesCheck>();
        services.AddSingleton<IInstallationCheck, DataFilesCheck>();
        services.AddSingleton<IInstallationCheck, WritePermissionCheck>();
        services.AddSingleton<CompositeInstallationCheck>();
        services.AddSingleton<IExePatcher, ExePatcher>();
        services.AddSingleton<IFirstTimeSetup, FirstTimeSetup>();
        services.AddSingleton<IInstallationStatus, InstallationStatus>();
        services.AddSingleton<GameRestorer>();

        services.AddSingleton<IEnemyConfigRepository, EnemyConfigRepository>();
        services.AddSingleton<IGameDataRepository, Data.GameDataRepository>();
        services.AddSingleton<ILastConfigStore, JsonLastConfigStore>();
        services.AddSingleton<PreviewRandomizerService>();
        services.AddSingleton<RandomizationRun>();
        services.AddSingleton<IRandomizerService, RandomizerService>();
        return services;
    }
}
