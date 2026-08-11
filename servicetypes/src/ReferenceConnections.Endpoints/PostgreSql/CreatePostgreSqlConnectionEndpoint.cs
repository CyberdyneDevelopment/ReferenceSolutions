using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.PostgreSql;
using Fdw.Services.Connections.PostgreSql.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceConnections.Endpoints;

/// <summary>
/// PostgreSql closure for the create connection endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreatePostgreSqlConnectionEndpoint : CreateConnectionEndpointBase<PostgreSqlConnectionConfiguration>
{
    private readonly ILogger<CreatePostgreSqlConnectionEndpoint> _logger;

    /// <inheritdoc />
    public CreatePostgreSqlConnectionEndpoint(
        ConnectionConfigurationProvider connectionProvider,
        ILogger<CreatePostgreSqlConnectionEndpoint> logger)
        : base(connectionProvider)
    {
        _logger = logger ?? NullLogger<CreatePostgreSqlConnectionEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: the global RoutePrefix is already "api/v1" (Program.cs), so repeating it here produced
        // /api/v1/api/v1/connections/... — the route existed but nothing could reach it (API-48).
        Post("connections/postgresql");
        Tags("Connections");
    }

    /// <inheritdoc />
    protected override ConnectionConfiguration CreateConnectionRecord(CreateConnectionRequest request, Guid connectionId)
    {
        return new ConnectionConfiguration
        {
            Id = connectionId,
            Name = request.Name,
            ServiceOptionType = "PostgreSql",
        };
    }

    /// <inheritdoc />
    protected override PostgreSqlConnectionConfiguration CreateTypedBody(CreateConnectionRequest request, Guid connectionId)
    {
        return new PostgreSqlConnectionConfiguration
        {
            // Why: Id is left as Guid.Empty — DefaultConfigurationProvider.Save mints it via
            // Guid.CreateVersion7() before INSERT. ConnectionId links this row to the parent.
            ConnectionId = connectionId,
            // Why: PostgreSql's typed body names the host field Host (not Server); the wizard sends
            // the hostname in the shared CreateConnectionRequest.Server field.
            Host = request.Server,
            Port = request.Port,
            Database = request.Database,
            AuthenticationType = request.AuthenticationType,
            AdditionalProperties = new Dictionary<string, string?>(request.Authentication, StringComparer.OrdinalIgnoreCase)
        };
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapToDetail(ConnectionConfiguration connection, PostgreSqlConnectionConfiguration typedBody, Guid connectionId)
    {
        return new ConnectionDetailDto
        {
            Id = connectionId,
            Name = connection.Name,
            ServiceType = "PostgreSql",
            Server = typedBody.Host,
            Port = typedBody.Port,
            Database = typedBody.Database,
            AuthenticationType = typedBody.AuthenticationType,
            Authentication = PostgreSqlAuthenticationTypes.ByName(typedBody.AuthenticationType)
                .MaskSecrets(new Dictionary<string, string?>(typedBody.AdditionalProperties, StringComparer.OrdinalIgnoreCase), ConnectionDetailDto.MaskedSecretValue),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
