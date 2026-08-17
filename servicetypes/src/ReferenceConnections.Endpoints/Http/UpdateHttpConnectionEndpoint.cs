using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceConnections.Endpoints;

/// <summary>
/// HTTP closure for the update connection endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateHttpConnectionEndpoint : UpdateConnectionEndpointBase<HttpConnectionConfiguration>
{
    private readonly ILogger<UpdateHttpConnectionEndpoint> _logger;

    /// <inheritdoc />
    public UpdateHttpConnectionEndpoint(
        ConnectionConfigurationProvider connectionProvider,
        ILogger<UpdateHttpConnectionEndpoint> logger)
        : base(connectionProvider)
    {
        _logger = logger ?? NullLogger<UpdateHttpConnectionEndpoint>.Instance;
    }

    /// <inheritdoc />
    // Why: type-specific route so that Http and MsSql update endpoints can coexist without conflict.
    protected override string Route => "/connections/http/{Name}";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapExistingToDetail(ConnectionConfiguration connection, HttpConnectionConfiguration body)
    {
        return new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = ConfigServiceType.Resolve(connection, _logger),
            Server = string.Empty,
            Database = string.Empty,
            BaseUrl = body.BaseUrl,
            Protocol = body.Protocol,
            TimeoutSeconds = body.TimeoutSeconds,
            AuthenticationType = body.AuthenticationType,
            UseMtls = body.UseMtls,
            IsActive = true,
            CreatedAt = DateTimeOffset.MinValue,
            UpdatedAt = DateTimeOffset.MinValue
        };
    }

    /// <inheritdoc />
    protected override (ConnectionConfiguration connection, HttpConnectionConfiguration body) MergeUpdate(
        UpdateConnectionRequest request,
        ConnectionConfiguration existingConnection,
        HttpConnectionConfiguration existingBody)
    {
        var mergedAuth = new Dictionary<string, string?>(existingBody.AdditionalProperties, StringComparer.OrdinalIgnoreCase);

        if (request.Authentication is not null)
        {
            foreach (var kvp in request.Authentication)
            {
                // If the incoming value is the mask placeholder, keep the existing value.
                if (string.Equals(kvp.Value, ConnectionDetailDto.MaskedSecretValue, StringComparison.Ordinal))
                    continue;
                mergedAuth[kvp.Key] = kvp.Value;
            }
        }

        // Why merged rather than replaced, same as auth above: a caller that sends one header means to
        // set that header, not to drop every other one the connection already carries.
        var mergedHeaders = new Dictionary<string, string?>(existingBody.Headers, StringComparer.OrdinalIgnoreCase);
        if (request.Headers is not null)
        {
            foreach (var kvp in request.Headers)
                mergedHeaders[kvp.Key] = kvp.Value;
        }

        // Why: parent connection record is not mutated — name updates are unsupported via this
        // endpoint (name is the route key). Only typed body fields are merged from the request.
        var updatedBody = new HttpConnectionConfiguration
        {
            Id = existingBody.Id,
            ConnectionId = existingBody.ConnectionId,
            BaseUrl = request.BaseUrl ?? existingBody.BaseUrl,
            Protocol = request.Protocol ?? existingBody.Protocol,
            TimeoutSeconds = request.TimeoutSeconds ?? existingBody.TimeoutSeconds,
            AuthenticationType = request.AuthenticationType ?? existingBody.AuthenticationType,
            AdditionalProperties = mergedAuth,
            Headers = mergedHeaders,
            UseMtls = request.UseMtls ?? existingBody.UseMtls,
            Lifetime = existingBody.Lifetime,
            ContentType = existingBody.ContentType,
            Soap = existingBody.Soap,
            Limits = existingBody.Limits
        };

        return (existingConnection, updatedBody);
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapUpdatedToDetail(ConnectionConfiguration connection, HttpConnectionConfiguration body)
    {
        return new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = ConfigServiceType.Resolve(connection, _logger),
            Server = string.Empty,
            Database = string.Empty,
            BaseUrl = body.BaseUrl,
            Protocol = body.Protocol,
            TimeoutSeconds = body.TimeoutSeconds,
            AuthenticationType = body.AuthenticationType,
            UseMtls = body.UseMtls,
            IsActive = true,
            CreatedAt = DateTimeOffset.MinValue,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
