using EnemyRandomizer.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Configuration;

public static class SettingsServiceCollectionExtensions
{
    /// <summary>Binds and validates every settings type. Invalid config fails on host start.</summary>
    public static IServiceCollection AddRandomizerSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedSettings<AppSettings>(configuration);
        services.AddValidatedSettings<GameDataSettings>(configuration);
        services.AddValidatedSettings<RandomizerOptionSettings>(configuration);
        services.AddValidatedSettings<DifficultyCurveSettings>(configuration);
        services.AddValidatedSettings<ReplacementRuleSettings>(configuration);
        services.AddValidatedSettings<MapPatchSettings>(configuration);
        services.AddValidatedSettings<MsbModelSettings>(configuration);
        services.AddValidatedSettings<MessageSettings>(configuration);
        return services;
    }

    /// <summary>Binds <typeparamref name="T"/> from its own section and registers <see cref="SettingsValidator{T}"/>.</summary>
    public static IServiceCollection AddValidatedSettings<T>(this IServiceCollection services, IConfiguration configuration)
        where T : class, IValidatableSettings
    {
        services.AddOptions<T>()
                .Bind(configuration.GetSection(T.SectionName), o => o.ErrorOnUnknownConfiguration = true)
                .ValidateOnStart();
        services.AddSingleton<IValidateOptions<T>, SettingsValidator<T>>();
        return services;
    }
}
