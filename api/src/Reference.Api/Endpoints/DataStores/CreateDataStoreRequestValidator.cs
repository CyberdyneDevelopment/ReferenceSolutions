using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Data.Endpoints;

namespace Reference.Api.Endpoints.DataStores;

/// <summary>
/// Validator for POST /datastores. Why: the framework Create endpoint base doesn't ship a
/// validator and FastEndpoints only scans the entry assembly, so the request flows to the
/// handler with empty fields and crashes with FDW-5364. Catching missing Name/ConnectionName
/// here turns those into structured 400 responses.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateDataStoreRequestValidator : Validator<CreateDataStoreRequest>
{
    /// <inheritdoc />
    public CreateDataStoreRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters")
            .MaximumLength(128).WithMessage("Name must not exceed 128 characters");

        RuleFor(x => x.ConnectionName)
            .NotEmpty().WithMessage("ConnectionName is required")
            .MaximumLength(128).WithMessage("ConnectionName must not exceed 128 characters");
    }
}
