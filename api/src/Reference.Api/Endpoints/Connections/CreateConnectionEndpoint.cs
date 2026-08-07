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

namespace Reference.Api.Endpoints;

/// <summary>
/// MsSql closure for the create connection endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateConnectionEndpoint : CreateConnectionEndpointBase<MsSqlConnectionConfiguration>
{
    private readonly ILogger<CreateConnectionEndpoint> _logger;

    /// <inheritdoc />
    public CreateConnectionEndpoint(
        ConnectionConfigurationProvider connectionProvider,
        ILogger<CreateConnectionEndpoint> logger)
        : base(connectionProvider)
    {
        _logger = logger ?? NullLogger<CreateConnectionEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }

    /// <inheritdoc />
    protected override MsSqlConnectionConfiguration CreateTypedBody(CreateConnectionRequest request, Guid connectionId)
    {
        return new MsSqlConnectionConfiguration
        {
            // Why: Id is left as Guid.Empty — DefaultConfigurationProvider.Save mints it via
            // Guid.CreateVersion7() before INSERT. ConnectionId links this row to the parent.
            ConnectionId = connectionId,
            Server = request.Server,
            Port = request.Port,
            Database = request.Database,
            AuthenticationType = request.AuthenticationType,
            AdditionalProperties = new Dictionary<string, string?>(request.Authentication, StringComparer.OrdinalIgnoreCase),
            TrustServerCertificate = request.TrustServerCertificate,
            Encrypt = request.Encrypt
        };
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapToDetail(ConnectionConfiguration connection, MsSqlConnectionConfiguration typedBody, Guid connectionId)
    {
        return new ConnectionDetailDto
        {
            Id = connectionId,
            Name = connection.Name,
            ServiceType = ConfigServiceType.Resolve(connection, _logger),
            Server = typedBody.Server,
            Port = typedBody.Port,
            Database = typedBody.Database,
            AuthenticationType = typedBody.AuthenticationType,
            Authentication = MsSqlAuthenticationTypes.ByName(typedBody.AuthenticationType)
                .MaskSecrets(new Dictionary<string, string?>(typedBody.AdditionalProperties, StringComparer.OrdinalIgnoreCase), ConnectionDetailDto.MaskedSecretValue),
            TrustServerCertificate = typedBody.TrustServerCertificate,
            Encrypt = typedBody.Encrypt,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
