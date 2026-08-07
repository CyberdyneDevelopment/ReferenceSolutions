using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Reference.Api.Endpoints;

/// <summary>
/// Type-agnostic get-connection endpoint. Renders any connection type by reading the polymorphic
/// typed body off the header (populated by the configuration provider) and dispatching the
/// type-specific projection to the <see cref="IConnectionDetailMapper"/> registered for the
/// connection's ServiceOptionType.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetConnectionEndpoint : GetConnectionEndpointBase
{
    // Why: per-type projection registry keyed by ServiceOptionType — the sanctioned alternative to a
    // type switch (mirrors the connection provider's _typedProviders dictionary). One GET-by-name
    // endpoint renders every connection type. Mappers are stateless; the app declares the set it
    // renders, parallel to its per-type create endpoints.
    private static readonly Dictionary<string, IConnectionDetailMapper> Mappers =
        new IConnectionDetailMapper[]
        {
            new MsSqlConnectionDetailMapper(),
            new HttpConnectionDetailMapper(),
            new PostgreSqlConnectionDetailMapper(),
            new FileSystemConnectionDetailMapper(),
            new RoslynWorkspaceConnectionDetailMapper(),
        }.ToDictionary(m => m.ServiceOptionType, StringComparer.OrdinalIgnoreCase);

    private readonly ILogger<GetConnectionEndpoint> _logger;

    /// <inheritdoc />
    public GetConnectionEndpoint(
        ConnectionConfigurationProvider configProvider,
        ILogger<GetConnectionEndpoint> logger)
        : base(configProvider)
    {
        _logger = logger ?? NullLogger<GetConnectionEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapToDetail(ConnectionConfiguration connection, IConnectionConfiguration? body)
    {
        var dto = new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = ConfigServiceType.Resolve(connection, _logger),
            Server = string.Empty,
            Database = string.Empty,
            IsActive = true,
            CreatedAt = DateTimeOffset.MinValue,
            UpdatedAt = DateTimeOffset.MinValue,
            // Why (FDW-623): last-test status is no longer a column on the connection config — it
            // lives in conn.ConnectionHealthCheck. These fields are left unset (null) here; current
            // health is served by the connection health endpoint / conn.ConnectionHealthCurrent view.
            HealthCheckEnabled = connection.HealthCheckEnabled,
            HealthCheckOnStartup = connection.HealthCheckOnStartup,
            HealthCheckIntervalSeconds = connection.HealthCheckIntervalSeconds
        };

        // Why: fill type-specific fields by dispatching on ServiceOptionType. Header-only render when
        // there is no typed body, or no mapper registered for the discriminator (unknown type).
        if (body is not null
            && !string.IsNullOrEmpty(connection.ServiceOptionType)
            && Mappers.TryGetValue(connection.ServiceOptionType, out var mapper))
        {
            mapper.Map(connection, body, dto);
        }

        return dto;
    }
}
