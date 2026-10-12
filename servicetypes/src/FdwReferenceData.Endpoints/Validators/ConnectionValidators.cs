using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;

namespace FdwReferenceConnections.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="CreateConnectionRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateConnectionRequestValidator : Validator<CreateConnectionRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateConnectionRequestValidator"/> class.
    /// </summary>
    /// <remarks>
    /// Why only the name and the type: what a connection of a given type must carry is that type's own
    /// rule, checked when its configuration is bound (IConnectionType.EndpointBehavior). This validator
    /// used to list the valid types and the rules of three of them, so any other type -- FileSystem,
    /// RoslynWorkspace, Sqlite, or one a consumer adds -- was refused before it reached the endpoint.
    /// </remarks>
    public CreateConnectionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Connection name is required")
            .MaximumLength(100)
            .WithMessage("Connection name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Connection name must start with a letter and contain only letters, numbers, underscores, or hyphens");

        RuleFor(x => x.ServiceType)
            .NotEmpty()
            .WithMessage("Service type is required")
            .Must(type => !ReferenceEquals(ConnectionTypes.ByName(type), ConnectionTypes.NotFound))
            .WithMessage(x => $"Unknown service type '{x.ServiceType}'. Valid types: {string.Join(", ", ConnectionTypes.All().Values.Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal))}");
    }
}

/// <summary>
/// Validator for <see cref="UpdateConnectionRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateConnectionRequestValidator : Validator<UpdateConnectionRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateConnectionRequestValidator"/> class.
    /// </summary>
    public UpdateConnectionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Connection name is required in the route");

        // Why: a request with an empty body or only the route-bound Name is a no-op. At least one
        // updateable property must be supplied so the caller gets a structured 400 instead of a
        // silent 200.
        //
        // Why every field and not the relational ones: this guard listed Server, Port, Database and
        // the rest of the MsSql set, so an HTTP connection updated with the fields it actually has —
        // a base url, a protocol, a timeout — supplied nothing the guard could see and was refused.
        // The request is shared across connection types; the check has to be too.
        //
        // Why the list is no longer written here (FDW-1177): this guard was a second hand-written copy of the update's members and
        // it left out the credentials, so a password could not be rotated by itself, and it counted values the body builder drops.
        // UpdateConnectionChanges holds the members once, with what counts as stated for each, and also words the refusal.
        RuleFor(x => x)
            .Must(request => UpdateConnectionChanges.Stated(request))
            .WithErrorCode(UpdateConnectionChanges.NothingStatedCode)
            .WithMessage(UpdateConnectionChanges.RefusalMessage);

        When(x => x.Server is not null, () =>
        {
            RuleFor(x => x.Server)
                .MaximumLength(255)
                .WithMessage("Server cannot exceed 255 characters");
        });

        When(x => x.Port.HasValue, () =>
        {
            RuleFor(x => x.Port!.Value)
                .InclusiveBetween(1, 65535)
                .WithMessage("Port must be between 1 and 65535");
        });

        When(x => x.Database is not null, () =>
        {
            RuleFor(x => x.Database)
                .MaximumLength(128)
                .WithMessage("Database name cannot exceed 128 characters");
        });

    }
}

/// <summary>
/// Validator for <see cref="TestConnectionRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TestConnectionRequestValidator : Validator<TestConnectionRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestConnectionRequestValidator"/> class.
    /// </summary>
    public TestConnectionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Connection name is required");
    }
}

/// <summary>
/// Validator for <see cref="ConnectionNameRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ConnectionNameRequestValidator : Validator<ConnectionNameRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionNameRequestValidator"/> class.
    /// </summary>
    public ConnectionNameRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Connection name is required");
    }
}
