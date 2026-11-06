using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.Endpoints.Validators;

namespace FdwReferenceConnections.Endpoints.Validators;

/// <summary>
/// The create-connection rules of this host: FDW's (<see cref="CreateConnectionRequestValidator"/>) plus the one rule only a host can state, that
/// the service type is one this host registered (FDW-3572).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReferenceCreateConnectionRequestValidator : CreateConnectionRequestValidator
{
    public ReferenceCreateConnectionRequestValidator()
    {
        // Why the guard: the FDW rule already answers an empty service type, and this one should not answer it a second time.
        When(x => !string.IsNullOrEmpty(x.ServiceType), () =>
            RuleFor(x => x.ServiceType)
                .Must(type => !ReferenceEquals(ConnectionTypes.ByName(type), ConnectionTypes.NotFound))
                .WithMessage(x => $"Unknown service type '{x.ServiceType}'. Valid types: {string.Join(", ", ConnectionTypes.All().Values.Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal))}"));
    }
}

/// <summary>The update-connection rules of this host: FDW's (<see cref="UpdateConnectionRequestValidator"/>), with nothing the host adds (FDW-3572).</summary>
[ExcludeFromCodeCoverage]
public sealed class ReferenceUpdateConnectionRequestValidator : UpdateConnectionRequestValidator;

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
