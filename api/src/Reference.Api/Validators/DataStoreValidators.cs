using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Clients.Models;

namespace Reference.Api.Validators;

/// <summary>
/// Validator for <see cref="DataStoreNameRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DataStoreNameRequestValidator : Validator<DataStoreNameRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreNameRequestValidator"/> class.
    /// </summary>
    public DataStoreNameRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("DataStore name is required");
    }
}

/// <summary>
/// Validator for <see cref="UpdateDataStoreRequest"/>.
/// Why: PUT with no updateable fields was returning 200 (no-op). Reject as 400.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateDataStoreRequestValidator : Validator<UpdateDataStoreRequest>
{
    /// <inheritdoc />
    public UpdateDataStoreRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("DataStore name is required in the route");

        RuleFor(x => x)
            .Must(req => req.StoreType is not null
                || req.ConnectionName is not null
                || req.Description is not null
                || req.WriteMode is not null)
            .WithMessage("At least one updateable field must be supplied (StoreType, ConnectionName, Description, or WriteMode)");
    }
}

/// <summary>
/// Validator for <see cref="GetContainerRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetContainerRequestValidator : Validator<GetContainerRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetContainerRequestValidator"/> class.
    /// </summary>
    public GetContainerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("DataStore name is required");

        RuleFor(x => x.PathName)
            .NotEmpty()
            .WithMessage("Path name is required");

        RuleFor(x => x.ContainerName)
            .NotEmpty()
            .WithMessage("Container name is required");
    }
}

/// <summary>
/// Validator for <see cref="DiscoverDataStoreRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DiscoverDataStoreRequestValidator : Validator<DiscoverDataStoreRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverDataStoreRequestValidator"/> class.
    /// </summary>
    public DiscoverDataStoreRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("DataStore name is required for discovery");
    }
}
