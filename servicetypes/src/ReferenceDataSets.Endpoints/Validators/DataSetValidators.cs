using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Data.Endpoints;

namespace ReferenceDataSets.Endpoints.Validators;

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
    }
}
