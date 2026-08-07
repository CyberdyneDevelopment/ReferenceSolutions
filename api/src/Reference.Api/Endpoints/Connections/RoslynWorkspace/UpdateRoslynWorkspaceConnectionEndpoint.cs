using System;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.RoslynWorkspace;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Reference.Api.Endpoints;

/// <summary>
/// RoslynWorkspace closure for the update connection endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateRoslynWorkspaceConnectionEndpoint : UpdateConnectionEndpointBase<RoslynWorkspaceConnectionConfiguration>
{
    private readonly ILogger<UpdateRoslynWorkspaceConnectionEndpoint> _logger;

    /// <inheritdoc />
    public UpdateRoslynWorkspaceConnectionEndpoint(
        ConnectionConfigurationProvider connectionProvider,
        ILogger<UpdateRoslynWorkspaceConnectionEndpoint> logger)
        : base(connectionProvider)
    {
        _logger = logger ?? NullLogger<UpdateRoslynWorkspaceConnectionEndpoint>.Instance;
    }

    /// <inheritdoc />
    // Why: type-specific route so that RoslynWorkspace and MsSql update endpoints can coexist without conflict.
    protected override string Route => "/connections/roslynworkspace/{Name}";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapExistingToDetail(ConnectionConfiguration connection, RoslynWorkspaceConnectionConfiguration body)
    {
        return new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = ConfigServiceType.Resolve(connection, _logger),
            Server = string.Empty,
            Database = string.Empty,
            // Why: SolutionPath maps to BaseUrl and ModeName maps to Protocol for DTO consistency.
            BaseUrl = body.SolutionPath,
            Protocol = body.ModeName,
            IsActive = true,
            CreatedAt = DateTimeOffset.MinValue,
            UpdatedAt = DateTimeOffset.MinValue
        };
    }

    /// <inheritdoc />
    protected override (ConnectionConfiguration connection, RoslynWorkspaceConnectionConfiguration body) MergeUpdate(
        UpdateConnectionRequest request,
        ConnectionConfiguration existingConnection,
        RoslynWorkspaceConnectionConfiguration existingBody)
    {
        // Why: SolutionPath is carried in request.BaseUrl; ModeName in request.Protocol.
        var updatedBody = new RoslynWorkspaceConnectionConfiguration
        {
            Id = existingBody.Id,
            ConnectionId = existingBody.ConnectionId,
            SolutionPath = request.BaseUrl ?? existingBody.SolutionPath,
            ModeName = request.Protocol ?? existingBody.ModeName,
            ExcludePatterns = existingBody.ExcludePatterns
        };

        return (existingConnection, updatedBody);
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapUpdatedToDetail(ConnectionConfiguration connection, RoslynWorkspaceConnectionConfiguration body)
    {
        return new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = ConfigServiceType.Resolve(connection, _logger),
            Server = string.Empty,
            Database = string.Empty,
            BaseUrl = body.SolutionPath,
            Protocol = body.ModeName,
            IsActive = true,
            CreatedAt = DateTimeOffset.MinValue,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
