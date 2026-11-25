using EnemyRandomizer.Core.Configuration;
using Microsoft.Extensions.Options;

namespace EnemyRandomizer.Infrastructure.Configuration;

/// <summary>
/// Adapter (bridge between Core and Microsoft.Extensions.Options): one generic validator for every
/// <see cref="IValidatableSettings"/> type, so Core stays free of framework dependencies.
/// </summary>
public sealed class SettingsValidator<T> : IValidateOptions<T>
    where T : class, IValidatableSettings
{
    public ValidateOptionsResult Validate(string? name, T options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var errors = options.Validate()
                            .Select(e => $"[{T.SectionName}] {e}")
                            .ToList();

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
