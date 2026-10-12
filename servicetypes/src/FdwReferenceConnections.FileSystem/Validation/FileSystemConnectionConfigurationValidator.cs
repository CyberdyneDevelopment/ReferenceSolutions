using FluentValidation;
using Fdw.Validation;

namespace FdwReferenceConnections.FileSystem.Validation;

/// <summary>
/// Validator for <see cref="FileSystemConnectionConfiguration"/>.
/// </summary>
public sealed class FileSystemConnectionConfigurationValidator : FdwConfigurationValidator<FileSystemConnectionConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConnectionConfigurationValidator"/> class.
    /// </summary>
    public FileSystemConnectionConfigurationValidator()
    {
        // Why: Name is a header field on IConnectionImplementationConfiguration after config-split.
        // FileSystemConnectionConfiguration exposes it as an explicit IGenericConfiguration member
        // returning string.Empty — it cannot be validated here.

        RuleFor(x => x.Root)
            .NotEmpty()
            .WithMessage("Root is required");
    }
}
