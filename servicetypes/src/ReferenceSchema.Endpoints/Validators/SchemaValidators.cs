using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;
using Fdw.Schema.Endpoints.Discovery;

namespace ReferenceSchema.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="DiscoverSchemaRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DiscoverSchemaRequestValidator : Validator<DiscoverSchemaRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverSchemaRequestValidator"/> class.
    /// </summary>
    public DiscoverSchemaRequestValidator()
    {
        RuleFor(x => x.ConnectionName)
            .NotEmpty()
            .WithMessage("Connection name is required");
    }
}

/// <summary>
/// Validator for <see cref="DataPreviewRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DataPreviewRequestValidator : Validator<DataPreviewRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataPreviewRequestValidator"/> class.
    /// </summary>
    public DataPreviewRequestValidator()
    {
        // Why: FDW base accepts either DataSetName or ContainerName (with DataStoreName+PathName).
        // Validate the same invariant the base enforces in HandleAsync so the error is a 400
        // (FluentValidation) rather than a ThrowError inside the handler.
        RuleFor(x => x)
            .Must(req => !string.IsNullOrEmpty(req.DataSetName) || !string.IsNullOrEmpty(req.ContainerName))
            .WithMessage("Either DataSetName or ContainerName must be provided");

        RuleFor(x => x.MaxRows)
            .InclusiveBetween(1, 1000)
            .WithMessage("MaxRows must be between 1 and 1000");

        When(x => !string.IsNullOrEmpty(x.ContainerName) && string.IsNullOrEmpty(x.DataSetName), () =>
        {
            RuleFor(x => x.DataStoreName)
                .NotEmpty()
                .WithMessage("DataStoreName is required when ContainerName is provided");

            RuleFor(x => x.PathName)
                .NotEmpty()
                .WithMessage("PathName is required when ContainerName is provided");
        });

        When(x => x.Columns != null && x.Columns.Count > 0, () =>
        {
            RuleFor(x => x.Columns!.Count)
                .LessThanOrEqualTo(50)
                .WithMessage("Cannot request more than 50 columns");

            RuleForEach(x => x.Columns)
                .NotEmpty()
                .WithMessage("Column names cannot be empty")
                .MaximumLength(128)
                .WithMessage("Column names cannot exceed 128 characters");
        });
    }
}
