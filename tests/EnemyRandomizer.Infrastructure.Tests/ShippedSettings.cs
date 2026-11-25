using EnemyRandomizer.Core.Configuration;
using EnemyRandomizer.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Tests;

/// <summary>Binds settings from the JSON files shipped with the app.</summary>
internal static class ShippedSettings
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddRandomizerConfigFiles(AppContext.BaseDirectory)
        .Build();

    public static T Get<T>()
        where T : class, IValidatableSettings, new() =>
        Configuration.GetSection(T.SectionName).Get<T>() ?? new T();

    public static IOptions<T> Options<T>()
        where T : class, IValidatableSettings, new() =>
        Microsoft.Extensions.Options.Options.Create(Get<T>());

    /// <summary>Repository root (folder with the solution file).</summary>
    public static string RepositoryRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DarkSoulsEnemyRandomizer.slnx")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }
}
