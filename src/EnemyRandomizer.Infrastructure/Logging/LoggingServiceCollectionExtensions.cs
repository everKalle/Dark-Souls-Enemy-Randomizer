using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EnemyRandomizer.Infrastructure.Logging;

public static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// Serilog with levels from the <c>Serilog</c> config section and a daily rolling file in
    /// <paramref name="logDirectory"/> (<c>app-YYYYMMDD.log</c>, last 7 kept).
    /// </summary>
    public static IServiceCollection AddRandomizerLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        string logDirectory)
    {
        services.AddSerilog(logger => logger
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(logDirectory, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                formatProvider: System.Globalization.CultureInfo.InvariantCulture));
        return services;
    }
}
