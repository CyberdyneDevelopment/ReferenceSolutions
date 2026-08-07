using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.FileSystem;

namespace Reference.Api.Endpoints;

/// <summary>Projects a FileSystem typed body onto the detail DTO (mirrors CreateFileSystemConnectionEndpoint.MapToDetail).</summary>
[ExcludeFromCodeCoverage]
internal sealed class FileSystemConnectionDetailMapper : IConnectionDetailMapper
{
    /// <inheritdoc />
    public string ServiceOptionType => "FileSystem";

    /// <inheritdoc />
    public void Map(ConnectionConfiguration parent, IConnectionConfiguration body, ConnectionDetailDto dto)
    {
        // Why: FileSystem has no Server/Database; the create endpoint surfaces Root via BaseUrl, so GET
        // mirrors that to keep create and get responses identical.
        var c = (FileSystemConnectionConfiguration)body;
        dto.BaseUrl = c.Root;
    }
}
