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
/// RoslynWorkspace closure for the create connection endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateRoslynWorkspaceConnectionEndpoint : CreateConnectionEndpointBase<RoslynWorkspaceConnectionConfiguration>
{
    private readonly ILogger<CreateRoslynWorkspaceConnectionEndpoint> _logger;

    /// <inheritdoc />
    public CreateRoslynWorkspaceConnectionEndpoint(
        ConnectionConfigurationProvider connectionProvider,
        ILogger<CreateRoslynWorkspaceConnectionEndpoint> logger)
        : base(connectionProvider)
    {
        _logger = logger ?? NullLogger<CreateRoslynWorkspaceConnectionEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: the global RoutePrefix is already "api/v1" (Program.cs), so repeating it here produced
        // /api/v1/api/v1/connections/... — the route existed but nothing could reach it (API-48).
        Post("connections/roslynworkspace");
        Tags("Connections");
    }

    /// <inheritdoc />
    protected override ConnectionConfiguration CreateConnectionRecord(CreateConnectionRequest request, Guid connectionId)
    {
        return new ConnectionConfiguration
        {
            Id = connectionId,
            Name = request.Name,
            ServiceOptionType = "RoslynWorkspace",
        };
    }

    /// <inheritdoc />
    protected override RoslynWorkspaceConnectionConfiguration CreateTypedBody(CreateConnectionRequest request, Guid connectionId)
    {
        return new RoslynWorkspaceConnectionConfiguration
        {
            // Why: Id is left as Guid.Empty — DefaultConfigurationProvider.Save mints it via
            // Guid.CreateVersion7() before INSERT. ConnectionId links this row to the parent.
            ConnectionId = connectionId,
            // Why: the .sln/.slnx path is carried in the shared CreateConnectionRequest.BaseUrl
            // field (the generic location slot), and the workspace mode in Protocol.
            SolutionPath = request.BaseUrl ?? string.Empty,
            ModeName = request.Protocol ?? string.Empty
        };
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapToDetail(ConnectionConfiguration connection, RoslynWorkspaceConnectionConfiguration typedBody, Guid connectionId)
    {
        return new ConnectionDetailDto
        {
            Id = connectionId,
            Name = connection.Name,
            ServiceType = "RoslynWorkspace",
            Server = string.Empty,
            Database = string.Empty,
            BaseUrl = typedBody.SolutionPath,
            Protocol = typedBody.ModeName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
