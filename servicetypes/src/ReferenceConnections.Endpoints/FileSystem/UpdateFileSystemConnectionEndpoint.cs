using System;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.FileSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceConnections.Endpoints;

/// <summary>
/// FileSystem closure for the update connection endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateFileSystemConnectionEndpoint : UpdateConnectionEndpointBase<FileSystemConnectionConfiguration>
{
    private readonly ILogger<UpdateFileSystemConnectionEndpoint> _logger;

    /// <inheritdoc />
    public UpdateFileSystemConnectionEndpoint(
        ConnectionConfigurationProvider connectionProvider,
        ILogger<UpdateFileSystemConnectionEndpoint> logger)
        : base(connectionProvider)
    {
        _logger = logger ?? NullLogger<UpdateFileSystemConnectionEndpoint>.Instance;
    }

    /// <inheritdoc />
    // Why: type-specific route so that FileSystem and MsSql update endpoints can coexist without conflict.
    protected override string Route => "/connections/filesystem/{Name}";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapExistingToDetail(ConnectionConfiguration connection, FileSystemConnectionConfiguration body)
    {
        return new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = ConfigServiceType.Resolve(connection, _logger),
            Server = string.Empty,
            Database = string.Empty,
            // Why: FileSystem root path is surfaced in BaseUrl for DTO consistency with the create response.
            BaseUrl = body.Root,
            IsActive = true,
            CreatedAt = DateTimeOffset.MinValue,
            UpdatedAt = DateTimeOffset.MinValue
        };
    }

    /// <inheritdoc />
    protected override (ConnectionConfiguration connection, FileSystemConnectionConfiguration body) MergeUpdate(
        UpdateConnectionRequest request,
        ConnectionConfiguration existingConnection,
        FileSystemConnectionConfiguration existingBody)
    {
        // Why: FileSystem root path is carried in request.BaseUrl (the generic location slot).
        var updatedBody = new FileSystemConnectionConfiguration
        {
            Id = existingBody.Id,
            ConnectionId = existingBody.ConnectionId,
            Root = request.BaseUrl ?? existingBody.Root
        };

        return (existingConnection, updatedBody);
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapUpdatedToDetail(ConnectionConfiguration connection, FileSystemConnectionConfiguration body)
    {
        return new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = ConfigServiceType.Resolve(connection, _logger),
            Server = string.Empty,
            Database = string.Empty,
            BaseUrl = body.Root,
            IsActive = true,
            CreatedAt = DateTimeOffset.MinValue,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
