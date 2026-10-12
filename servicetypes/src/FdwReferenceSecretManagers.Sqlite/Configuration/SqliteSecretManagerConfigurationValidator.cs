using FluentValidation;

namespace FdwReferenceSecretManagers.Sqlite.Configuration;

/// <summary>
/// Validator for <see cref="SqliteSecretManagerConfiguration"/>.
/// </summary>
public sealed class SqliteSecretManagerConfigurationValidator : AbstractValidator<SqliteSecretManagerConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSecretManagerConfigurationValidator"/> class.
    /// </summary>
    public SqliteSecretManagerConfigurationValidator()
    {
        // Why: Name is a header field on SecretManagerConfiguration after config-split.
        // SqliteSecretManagerConfiguration exposes it as an explicit IGenericConfiguration member
        // returning string.Empty — it cannot be validated here.

        RuleFor(x => x.DataSource)
            .NotEmpty()
            .WithMessage("DataSource (SQLite file path) is required for the SQLite secret manager.");

        RuleFor(x => x.TableName)
            .NotEmpty()
            .WithMessage("Table name is required.")
            .MaximumLength(128)
            .WithMessage("Table name cannot exceed 128 characters.")
            .Matches(@"^[a-zA-Z_][a-zA-Z0-9_]*$")
            .WithMessage("Table name must be a valid SQLite identifier.");

        RuleFor(x => x.CommandTimeoutSeconds)
            .InclusiveBetween(1, 300)
            .WithMessage("Command timeout must be between 1 and 300 seconds.");
    }
}
