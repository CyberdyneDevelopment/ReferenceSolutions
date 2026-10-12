using FluentValidation;

namespace FdwReferenceSecretManagers.MsSql.Configuration;

/// <summary>
/// Validator for <see cref="MsSqlSecretManagerConfiguration"/>.
/// </summary>
public sealed class MsSqlSecretManagerConfigurationValidator : AbstractValidator<MsSqlSecretManagerConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlSecretManagerConfigurationValidator"/> class.
    /// </summary>
    public MsSqlSecretManagerConfigurationValidator()
    {
        // Why: Name is a header field on SecretManagerConfiguration after config-split.
        // MsSqlSecretManagerConfiguration exposes it as an explicit IGenericConfiguration member
        // returning string.Empty — it cannot be validated here.

        RuleFor(x => x.Server)
            .NotEmpty()
            .WithMessage("Server is required for MsSql secret manager.");

        RuleFor(x => x.Database)
            .NotEmpty()
            .WithMessage("Database is required for MsSql secret manager.");

        RuleFor(x => x.Schema)
            .NotEmpty()
            .WithMessage("Schema name is required.")
            .MaximumLength(128)
            .WithMessage("Schema name cannot exceed 128 characters.")
            .Matches(@"^[a-zA-Z_][a-zA-Z0-9_]*$")
            .WithMessage("Schema name must be a valid SQL identifier.");

        RuleFor(x => x.TableName)
            .NotEmpty()
            .WithMessage("Table name is required.")
            .MaximumLength(128)
            .WithMessage("Table name cannot exceed 128 characters.")
            .Matches(@"^[a-zA-Z_][a-zA-Z0-9_]*$")
            .WithMessage("Table name must be a valid SQL identifier.");

        RuleFor(x => x.CommandTimeoutSeconds)
            .InclusiveBetween(1, 300)
            .WithMessage("Command timeout must be between 1 and 300 seconds.");
    }
}
