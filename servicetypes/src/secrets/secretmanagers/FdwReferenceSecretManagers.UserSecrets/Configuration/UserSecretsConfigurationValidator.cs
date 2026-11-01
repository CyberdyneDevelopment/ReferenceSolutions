using System;
using System.Linq;
using FluentValidation;

namespace FdwReferenceSecretManagers.UserSecrets.Configuration;

/// <summary>
/// Validator for <see cref="UserSecretsConfiguration"/>.
/// </summary>
public sealed class UserSecretsConfigurationValidator : AbstractValidator<UserSecretsConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSecretsConfigurationValidator"/> class.
    /// </summary>
    public UserSecretsConfigurationValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.UserSecretsId) || !string.IsNullOrWhiteSpace(x.SecretsFilePath))
            .WithMessage("Either UserSecretsId or SecretsFilePath must be specified");

        When(x => !string.IsNullOrWhiteSpace(x.UserSecretsId), () =>
        {
            RuleFor(x => x.UserSecretsId)
                .Must(BeValidUserSecretsId!)
                .WithMessage("UserSecretsId must be a valid identifier (typically a GUID)");
        });

        When(x => !string.IsNullOrWhiteSpace(x.SecretsFilePath), () =>
        {
            RuleFor(x => x.SecretsFilePath)
                .Must(BeValidFilePath!)
                .WithMessage("SecretsFilePath must be a valid file path ending with .json");
        });
    }

    private static bool BeValidUserSecretsId(string userSecretsId)
    {
        // User Secrets ID is typically a GUID, but can also be any non-empty string
        // that doesn't contain invalid path characters
        if (string.IsNullOrWhiteSpace(userSecretsId))
            return false;

        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        return !userSecretsId.Any(c => invalidChars.Contains(c));
    }

    private static bool BeValidFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        // Must end with .json
        if (!filePath.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
            return false;

        // Check for invalid path characters
        try
        {
            var invalidChars = System.IO.Path.GetInvalidPathChars();
            return !filePath.Any(c => invalidChars.Contains(c));
        }
        catch (Exception ex)
        {
            // Why: Path.GetInvalidPathChars() is not expected to throw on any supported platform,
            // but observe ex so the failure is not silently discarded if it ever does.
            _ = ex;
            return false;
        }
    }
}
