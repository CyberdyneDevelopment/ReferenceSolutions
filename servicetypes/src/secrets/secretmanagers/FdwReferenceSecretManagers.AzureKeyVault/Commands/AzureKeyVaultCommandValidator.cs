using FluentValidation;

namespace FdwReferenceSecretManagers.AzureKeyVault.Commands;

/// <summary>
/// Validates Azure Key Vault secret management commands.
/// </summary>
public sealed class AzureKeyVaultCommandValidator : AbstractValidator<AzureKeyVaultManagementCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultCommandValidator"/> class.
    /// </summary>
    public AzureKeyVaultCommandValidator()
    {
        RuleFor(x => x.CommandType)
            .NotEmpty()
            .WithMessage("ManagementCommand type is required.");

        RuleFor(x => x.SecretKey)
            .NotEmpty()
            .When(x => x.CommandType is not "ListSecrets")
            .WithMessage("Secret key is required for this operation.");

        RuleFor(x => x.Parameters)
            .NotNull()
            .WithMessage("ManagementCommand parameters must be initialized.");
    }
}