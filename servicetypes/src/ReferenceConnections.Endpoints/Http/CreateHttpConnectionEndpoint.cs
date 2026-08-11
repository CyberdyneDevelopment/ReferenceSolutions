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
/// HTTP closure for the create connection endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateHttpConnectionEndpoint : CreateConnectionEndpointBase<HttpConnectionConfiguration>
{
    private readonly ILogger<CreateHttpConnectionEndpoint> _logger;

    /// <inheritdoc />
    public CreateHttpConnectionEndpoint(
        ConnectionConfigurationProvider connectionProvider,
        ILogger<CreateHttpConnectionEndpoint> logger)
        : base(connectionProvider)
    {
        _logger = logger ?? NullLogger<CreateHttpConnectionEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: The global RoutePrefix is already "api/v1" (set in Program.cs); including "api/v1"
        // here produces a double-prefixed route /api/v1/api/v1/connections/http (API-48 bug fix).
        Post("connections/http");
        Tags("Connections");
    }

    /// <inheritdoc />
    protected override ConnectionConfiguration CreateConnectionRecord(CreateConnectionRequest request, Guid connectionId)
    {
        return new ConnectionConfiguration
        {
            Id = connectionId,
            Name = request.Name,
            ServiceOptionType = "Http",
        };
    }

    /// <inheritdoc />
    protected override HttpConnectionConfiguration CreateTypedBody(CreateConnectionRequest request, Guid connectionId)
    {
        return new HttpConnectionConfiguration
        {
            // Why: Id is left as Guid.Empty — DefaultConfigurationProvider.Save mints it via
            // Guid.CreateVersion7() before INSERT. ConnectionId links this row to the parent.
            ConnectionId = connectionId,
            BaseUrl = request.BaseUrl ?? string.Empty,
            Protocol = request.Protocol ?? "Rest",
            TimeoutSeconds = request.TimeoutSeconds ?? 30,
            AuthenticationType = request.AuthenticationType ?? "None",
            AdditionalProperties = request.Authentication != null
                ? new Dictionary<string, string?>(request.Authentication, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
            UseMtls = request.UseMtls
        };
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapToDetail(ConnectionConfiguration connection, HttpConnectionConfiguration typedBody, Guid connectionId)
    {
        return new ConnectionDetailDto
        {
            Id = connectionId,
            Name = connection.Name,
            ServiceType = "Http",
            Server = string.Empty,
            Database = string.Empty,
            BaseUrl = typedBody.BaseUrl,
            Protocol = typedBody.Protocol,
            TimeoutSeconds = typedBody.TimeoutSeconds,
            AuthenticationType = typedBody.AuthenticationType,
            UseMtls = typedBody.UseMtls,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
