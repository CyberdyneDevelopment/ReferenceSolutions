using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Data.Endpoints;

namespace FdwReferenceDataSets.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="DataSetNameRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DataSetNameRequestValidator : Validator<DataSetNameRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetNameRequestValidator"/> class.
    /// </summary>
    public DataSetNameRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("DataSet name is required");
    }
}

/// <summary>
/// Validator for <see cref="CreateDataSetRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateDataSetRequestValidator : Validator<CreateDataSetRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateDataSetRequestValidator"/> class.
    /// </summary>
    public CreateDataSetRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("DataSet name is required")
            .MaximumLength(100)
            .WithMessage("DataSet name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("DataSet name must start with a letter and contain only letters, numbers, underscores, or hyphens");

        RuleFor(x => x.Version)
            .NotEmpty()
            .WithMessage("Version is required")
            .Matches(@"^\d+\.\d+(\.\d+)?$")
            .WithMessage("Version must be in format X.Y or X.Y.Z (e.g., 1.0 or 1.0.0)");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters");
        });

        When(x => !string.IsNullOrEmpty(x.RecordTypeName), () =>
        {
            RuleFor(x => x.RecordTypeName)
                .MaximumLength(200)
                .WithMessage("Record type name cannot exceed 200 characters");
        });

        // There is no default: every field states whether callers may filter on it (FDW-1102).
        RuleForEach(x => x.Fields)
            .Must(field => field?.IsFilterable is not null)
            .WithMessage((_, field) => $"Field '{field?.Name}' must state isFilterable (true or false); there is no default");
    }
}

/// <summary>
/// Validator for <see cref="UpdateDataSetRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateDataSetRequestValidator : Validator<UpdateDataSetRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDataSetRequestValidator"/> class.
    /// </summary>
    public UpdateDataSetRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("DataSet name is required in the route");

        When(x => !string.IsNullOrEmpty(x.Version), () =>
        {
            RuleFor(x => x.Version)
                .Matches(@"^\d+\.\d+(\.\d+)?$")
                .WithMessage("Version must be in format X.Y or X.Y.Z (e.g., 1.0 or 1.0.0)");
        });

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters");
        });

        When(x => !string.IsNullOrEmpty(x.RecordTypeName), () =>
        {
            RuleFor(x => x.RecordTypeName)
                .MaximumLength(200)
                .WithMessage("Record type name cannot exceed 200 characters");
        });

        // A request that carries fields replaces them, so each states whether callers may filter on it (FDW-1102).
        When(x => x.Fields is not null, () =>
        {
            RuleForEach(x => x.Fields!)
                .Must(field => field?.IsFilterable is not null)
                .WithMessage((_, field) => $"Field '{field?.Name}' must state isFilterable (true or false); there is no default");
        });
    }
}

/// <summary>
/// Validator for <see cref="PreviewDataSetRequest"/>.
/// </summary>
/// <remarks>
/// Why this exists: <c>MaxRows</c> had no validator and <c>PreviewDataSetEndpointBase</c> does not
/// clamp it, so <c>?maxRows=5000000</c> materialised five million rows into dictionaries. A bound
/// the caller can exceed silently is not a bound.
/// <para>
/// It rejects rather than clamps. Clamping answers a request nobody made and reports success for it;
/// the caller asked for a number the endpoint will not serve, and saying so is the honest answer.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class PreviewDataSetRequestValidator : Validator<PreviewDataSetRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewDataSetRequestValidator"/> class.
    /// </summary>
    public PreviewDataSetRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("DataSet name is required");

        RuleFor(x => x.MaxRows)
            .InclusiveBetween(1, 1000)
            .WithMessage("MaxRows must be between 1 and 1000");
    }
}
