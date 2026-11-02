using FluentValidation;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Configuration;

/// <summary>
/// Validator for <see cref="EnvironmentVariableConfiguration"/>.
/// </summary>
public sealed class EnvironmentVariableConfigurationValidator : AbstractValidator<EnvironmentVariableConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableConfigurationValidator"/> class.
    /// </summary>
    public EnvironmentVariableConfigurationValidator()
    {
        // Why: Name is a header field on SecretManagerConfiguration after config-split.
        // EnvironmentVariableConfiguration exposes it as an explicit IGenericConfiguration member
        // returning string.Empty — it cannot be validated here.

        RuleFor(x => x.Separator)
            .NotEmpty()
            .WithMessage("Separator is required for nested key support.");

        // Prefix is required - no silent fallback to looking up raw secret keys
        RuleFor(x => x.Prefix)
            .NotEmpty()
            .WithMessage("Prefix is required. Environment variable secret manager must have an explicit prefix (e.g., 'FDW_SECRET_') to avoid accidentally exposing unrelated environment variables.");
    }
}
