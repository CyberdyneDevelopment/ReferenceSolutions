using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceConnections.Endpoints;

/// <summary>
/// MsSql closure for the update connection endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateConnectionEndpoint : UpdateConnectionEndpointBase<MsSqlConnectionConfiguration>
{
    private readonly ILogger<UpdateConnectionEndpoint> _logger;

    /// <inheritdoc />
    public UpdateConnectionEndpoint(
        ConnectionConfigurationProvider connectionProvider,
        ILogger<UpdateConnectionEndpoint> logger)
        : base(connectionProvider)
    {
        _logger = logger ?? NullLogger<UpdateConnectionEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapExistingToDetail(ConnectionConfiguration connection, MsSqlConnectionConfiguration body)
    {
        return new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = ConfigServiceType.Resolve(connection, _logger),
            Server = body.Server,
            Port = body.Port,
            Database = body.Database,
            AuthenticationType = body.AuthenticationType,
            Authentication = MsSqlAuthenticationTypes.ByName(body.AuthenticationType)
                .MaskSecrets(new Dictionary<string, string?>(body.AdditionalProperties, StringComparer.OrdinalIgnoreCase), ConnectionDetailDto.MaskedSecretValue),
            TrustServerCertificate = body.TrustServerCertificate,
            Encrypt = body.Encrypt,
            IsActive = true,
            CreatedAt = DateTimeOffset.MinValue,
            UpdatedAt = DateTimeOffset.MinValue
        };
    }

    /// <inheritdoc />
    protected override (ConnectionConfiguration connection, MsSqlConnectionConfiguration body) MergeUpdate(
        UpdateConnectionRequest request,
        ConnectionConfiguration existingConnection,
        MsSqlConnectionConfiguration existingBody)
    {
        var mergedAuth = new Dictionary<string, string?>(existingBody.AdditionalProperties, StringComparer.OrdinalIgnoreCase);

        if (request.Authentication is not null)
        {
            foreach (var kvp in request.Authentication)
            {
                // If the incoming value is the mask placeholder, keep the existing value
                if (string.Equals(kvp.Value, ConnectionDetailDto.MaskedSecretValue, StringComparison.Ordinal))
                    continue;
                mergedAuth[kvp.Key] = kvp.Value;
            }
        }

        // Why: parent connection record is returned as-is — name updates are not supported via this
        // endpoint (name is the route key). Only typed body fields are merged from the request.
        var updatedBody = new MsSqlConnectionConfiguration
        {
            Id = existingBody.Id,
            ConnectionId = existingBody.ConnectionId,
            Server = request.Server ?? existingBody.Server,
            Port = request.Port ?? existingBody.Port,
            Database = request.Database ?? existingBody.Database,
            AuthenticationType = request.AuthenticationType ?? existingBody.AuthenticationType,
            AdditionalProperties = mergedAuth,
            TrustServerCertificate = request.TrustServerCertificate ?? existingBody.TrustServerCertificate,
            Encrypt = request.Encrypt ?? existingBody.Encrypt,
            CommandTimeoutSeconds = existingBody.CommandTimeoutSeconds,
            ConnectionTimeoutSeconds = existingBody.ConnectionTimeoutSeconds,
            DefaultSchema = existingBody.DefaultSchema,
            EnableConnectionPooling = existingBody.EnableConnectionPooling,
            MinPoolSize = existingBody.MinPoolSize,
            MaxPoolSize = existingBody.MaxPoolSize,
            EnableMultipleActiveResultSets = existingBody.EnableMultipleActiveResultSets,
            ApplicationName = existingBody.ApplicationName,
            InstanceName = existingBody.InstanceName
        };

        return (existingConnection, updatedBody);
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapUpdatedToDetail(ConnectionConfiguration connection, MsSqlConnectionConfiguration body)
    {
        return new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = ConfigServiceType.Resolve(connection, _logger),
            Server = body.Server,
            Port = body.Port,
            Database = body.Database,
            AuthenticationType = body.AuthenticationType,
            Authentication = MsSqlAuthenticationTypes.ByName(body.AuthenticationType)
                .MaskSecrets(new Dictionary<string, string?>(body.AdditionalProperties, StringComparer.OrdinalIgnoreCase), ConnectionDetailDto.MaskedSecretValue),
            TrustServerCertificate = body.TrustServerCertificate,
            Encrypt = body.Encrypt,
            IsActive = true,
            CreatedAt = DateTimeOffset.MinValue,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
