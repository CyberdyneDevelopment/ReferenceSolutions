using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using FastEndpoints;
using FluentValidation;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.PostgreSql.Authentication;

namespace Reference.Api.Validators;

/// <summary>
/// Validator for <see cref="CreateConnectionRequest"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateConnectionRequestValidator : Validator<CreateConnectionRequest>
{
    private static readonly string[] ValidServiceTypes = ["MsSql", "PostgreSql", "Http"];

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateConnectionRequestValidator"/> class.
    /// </summary>
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
            .WithMessage("Service type is required (e.g., MsSql, PostgreSql, Http)")
            .Must(type => ValidServiceTypes.Any(v => string.Equals(v, type, StringComparison.OrdinalIgnoreCase)))
            .WithMessage(x => $"Unknown service type '{x.ServiceType}'. Valid types: {string.Join(", ", ValidServiceTypes)}");

        // Database connection validations (MsSql, PostgreSql)
        When(x => !string.Equals(x.ServiceType, "Http", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.Server)
                .NotEmpty()
                .WithMessage("Server hostname or IP address is required")
                .MaximumLength(255)
                .WithMessage("Server cannot exceed 255 characters");

            RuleFor(x => x.Port)
                .InclusiveBetween(1, 65535)
                .WithMessage("Port must be between 1 and 65535");

            RuleFor(x => x.Database)
                .NotEmpty()
                .WithMessage("Database name is required")
                .MaximumLength(128)
                .WithMessage("Database name cannot exceed 128 characters");
        });

        // MsSql-specific authentication validation
        When(x => string.Equals(x.ServiceType, "MsSql", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.AuthenticationType)
                .NotEmpty()
                .WithMessage("Authentication type is required")
                .Must(type => !string.IsNullOrEmpty(type) && MsSqlAuthenticationTypes.ByName(type) != null)
                .WithMessage(x => $"Unknown authentication type '{x.AuthenticationType}'. Valid types: {string.Join(", ", MsSqlAuthenticationTypes.All().Select(t => t.Name))}");

            When(x => string.Equals(x.AuthenticationType, "SqlAuth", StringComparison.Ordinal), () =>
            {
                RuleFor(x => x.Authentication)
                    .Must(auth => auth != null && auth.TryGetValue("Username", out var u) && !string.IsNullOrEmpty(u))
                    .WithMessage("Username is required for SQL authentication");

                RuleFor(x => x.Authentication)
                    .Must(auth => auth != null && auth.TryGetValue("SecretKeyName", out var s) && !string.IsNullOrEmpty(s))
                    .WithMessage("Secret key name is required for SQL authentication");
            });
        });

        // PostgreSql-specific authentication validation
        When(x => string.Equals(x.ServiceType, "PostgreSql", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.AuthenticationType)
                .NotEmpty()
                .WithMessage("Authentication type is required")
                .Must(type => !string.IsNullOrEmpty(type) && PostgreSqlAuthenticationTypes.ByName(type) != null)
                .WithMessage(x => $"Unknown authentication type '{x.AuthenticationType}'. Valid types: {string.Join(", ", PostgreSqlAuthenticationTypes.All().Select(t => t.Name))}");

            When(x => string.Equals(x.AuthenticationType, "Password", StringComparison.Ordinal), () =>
            {
                RuleFor(x => x.Authentication)
                    .Must(auth => auth != null && auth.TryGetValue("Username", out var u) && !string.IsNullOrEmpty(u))
                    .WithMessage("Username is required for Password authentication");

                RuleFor(x => x.Authentication)
                    .Must(auth => auth != null && auth.TryGetValue("SecretKeyName", out var s) && !string.IsNullOrEmpty(s))
                    .WithMessage("Secret key name is required for Password authentication");
            });
        });

        // HTTP-specific validation
        When(x => string.Equals(x.ServiceType, "Http", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.BaseUrl)
                .NotEmpty()
                .WithMessage("BaseUrl is required for HTTP connections")
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && (string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase)))
                .WithMessage("BaseUrl must be a valid absolute HTTP or HTTPS URL");

            RuleFor(x => x.Protocol)
                .NotEmpty()
                .WithMessage("Protocol is required for HTTP connections (e.g., Rest, Soap11, Soap12, GraphQL, OData)");

            When(x => x.TimeoutSeconds.HasValue, () =>
            {
                RuleFor(x => x.TimeoutSeconds!.Value)
                    .GreaterThan(0)
                    .WithMessage("TimeoutSeconds must be greater than 0")
                    .LessThanOrEqualTo(300)
                    .WithMessage("TimeoutSeconds cannot exceed 300");
            });
        });
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

        // Why: PUT with an empty body or only the route-bound Name field is a no-op. At least
        // one updateable property must be supplied so the caller gets a structured 400 instead
        // of a silent 200.
        RuleFor(x => x)
            .Must(req => req.Server is not null
                || req.Port.HasValue
                || req.Database is not null
                || req.AuthenticationType is not null
                || req.Authentication is not null
                || req.TrustServerCertificate.HasValue
                || req.Encrypt.HasValue
                || req.IsActive.HasValue)
            .WithMessage("At least one updateable field must be supplied (Server, Port, Database, AuthenticationType, Authentication, TrustServerCertificate, Encrypt, or IsActive)");

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

        When(x => x.AuthenticationType is not null, () =>
        {
            // AuthenticationType validation is only relevant for MsSql connections;
            // HTTP connections use SecurityType instead.
            RuleFor(x => x.AuthenticationType)
                .Must(type => string.IsNullOrEmpty(type) || MsSqlAuthenticationTypes.ByName(type!) != null)
                .WithMessage(x => $"Unknown authentication type '{x.AuthenticationType}'. Valid types: {string.Join(", ", MsSqlAuthenticationTypes.All().Select(t => t.Name))}");
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
