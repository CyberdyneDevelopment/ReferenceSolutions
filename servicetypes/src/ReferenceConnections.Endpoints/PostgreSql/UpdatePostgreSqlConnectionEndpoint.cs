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
/// PostgreSql closure for the update connection endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdatePostgreSqlConnectionEndpoint : UpdateConnectionEndpointBase<PostgreSqlConnectionConfiguration>
{
    private readonly ILogger<UpdatePostgreSqlConnectionEndpoint> _logger;

    /// <inheritdoc />
    public UpdatePostgreSqlConnectionEndpoint(
        ConnectionConfigurationProvider connectionProvider,
        ILogger<UpdatePostgreSqlConnectionEndpoint> logger)
        : base(connectionProvider)
    {
        _logger = logger ?? NullLogger<UpdatePostgreSqlConnectionEndpoint>.Instance;
    }

    /// <inheritdoc />
    // Why: type-specific route so that PostgreSql and MsSql update endpoints can coexist without conflict.
    protected override string Route => "/connections/postgresql/{Name}";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapExistingToDetail(ConnectionConfiguration connection, PostgreSqlConnectionConfiguration body)
    {
        return new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = ConfigServiceType.Resolve(connection, _logger),
            // Why: PostgreSql uses Host (not Server); surface it in the Server field for DTO consistency.
            Server = body.Host,
            Port = body.Port,
            Database = body.Database,
            AuthenticationType = body.AuthenticationType,
            Authentication = PostgreSqlAuthenticationTypes.ByName(body.AuthenticationType)
                .MaskSecrets(new Dictionary<string, string?>(body.AdditionalProperties, StringComparer.OrdinalIgnoreCase), ConnectionDetailDto.MaskedSecretValue),
            IsActive = true,
            CreatedAt = DateTimeOffset.MinValue,
            UpdatedAt = DateTimeOffset.MinValue
        };
    }

    /// <inheritdoc />
    protected override (ConnectionConfiguration connection, PostgreSqlConnectionConfiguration body) MergeUpdate(
        UpdateConnectionRequest request,
        ConnectionConfiguration existingConnection,
        PostgreSqlConnectionConfiguration existingBody)
    {
        var mergedAuth = new Dictionary<string, string?>(existingBody.AdditionalProperties, StringComparer.OrdinalIgnoreCase);

        if (request.Authentication is not null)
        {
            foreach (var kvp in request.Authentication)
            {
                if (string.Equals(kvp.Value, ConnectionDetailDto.MaskedSecretValue, StringComparison.Ordinal))
                    continue;
                mergedAuth[kvp.Key] = kvp.Value;
            }
        }

        // Why: parent connection record is not mutated — name is the route key.
        // PostgreSql host is carried in request.Server (the shared Server field).
        var updatedBody = new PostgreSqlConnectionConfiguration
        {
            Id = existingBody.Id,
            ConnectionId = existingBody.ConnectionId,
            Host = request.Server ?? existingBody.Host,
            Port = request.Port ?? existingBody.Port,
            Database = request.Database ?? existingBody.Database,
            AuthenticationType = request.AuthenticationType ?? existingBody.AuthenticationType,
            SslMode = existingBody.SslMode,
            CommandTimeout = existingBody.CommandTimeout,
            ConnectionTimeout = existingBody.ConnectionTimeout,
            DefaultSchema = existingBody.DefaultSchema,
            MaxPoolSize = existingBody.MaxPoolSize,
            MinPoolSize = existingBody.MinPoolSize,
            ApplicationName = existingBody.ApplicationName,
            AdditionalProperties = mergedAuth
        };

        return (existingConnection, updatedBody);
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapUpdatedToDetail(ConnectionConfiguration connection, PostgreSqlConnectionConfiguration body)
    {
        return new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = ConfigServiceType.Resolve(connection, _logger),
            Server = body.Host,
            Port = body.Port,
            Database = body.Database,
            AuthenticationType = body.AuthenticationType,
            Authentication = PostgreSqlAuthenticationTypes.ByName(body.AuthenticationType)
                .MaskSecrets(new Dictionary<string, string?>(body.AdditionalProperties, StringComparer.OrdinalIgnoreCase), ConnectionDetailDto.MaskedSecretValue),
            IsActive = true,
            CreatedAt = DateTimeOffset.MinValue,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
