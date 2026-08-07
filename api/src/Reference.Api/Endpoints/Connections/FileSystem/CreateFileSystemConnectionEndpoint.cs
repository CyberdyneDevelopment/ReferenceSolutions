using System;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.FileSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Reference.Api.Endpoints;

/// <summary>
/// FileSystem closure for the create connection endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateFileSystemConnectionEndpoint : CreateConnectionEndpointBase<FileSystemConnectionConfiguration>
{
    private readonly ILogger<CreateFileSystemConnectionEndpoint> _logger;

    /// <inheritdoc />
    public CreateFileSystemConnectionEndpoint(
        ConnectionConfigurationProvider connectionProvider,
        ILogger<CreateFileSystemConnectionEndpoint> logger)
        : base(connectionProvider)
    {
        _logger = logger ?? NullLogger<CreateFileSystemConnectionEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: the global RoutePrefix is already "api/v1" (Program.cs), so repeating it here produced
        // /api/v1/api/v1/connections/... — the route existed but nothing could reach it (API-48).
        Post("connections/filesystem");
        Tags("Connections");
    }

    /// <inheritdoc />
    protected override ConnectionConfiguration CreateConnectionRecord(CreateConnectionRequest request, Guid connectionId)
    {
        return new ConnectionConfiguration
        {
            Id = connectionId,
            Name = request.Name,
            ServiceOptionType = "FileSystem",
        };
    }

    /// <inheritdoc />
    protected override FileSystemConnectionConfiguration CreateTypedBody(CreateConnectionRequest request, Guid connectionId)
    {
        return new FileSystemConnectionConfiguration
        {
            // Why: Id is left as Guid.Empty — DefaultConfigurationProvider.Save mints it via
            // Guid.CreateVersion7() before INSERT. ConnectionId links this row to the parent.
            ConnectionId = connectionId,
            // Why: the root directory path is carried in the shared CreateConnectionRequest.BaseUrl
            // field — the same generic location slot HTTP uses for its base URL.
            Root = request.BaseUrl ?? string.Empty
        };
    }

    /// <inheritdoc />
    protected override ConnectionDetailDto MapToDetail(ConnectionConfiguration connection, FileSystemConnectionConfiguration typedBody, Guid connectionId)
    {
        return new ConnectionDetailDto
        {
            Id = connectionId,
            Name = connection.Name,
            ServiceType = "FileSystem",
            Server = string.Empty,
            Database = string.Empty,
            BaseUrl = typedBody.Root,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
