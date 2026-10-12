using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Endpoints;
using Fdw.WebMcp.Abstractions;
using FdwReferenceConnections.FileSystem.Registration;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The CreateFileSystemConnection endpoint.</summary>
[WebMcpTool(
    "create_file_system_connection",
    "Create file system connection.",
    PresetFieldName = nameof(CreateConnectionRequest.ServiceType),
    PresetOptionType = typeof(FileSystemConnectionType))]
[TypeOption(typeof(ConnectionEndpoints), "CreateFileSystemConnection")]
public class CreateFileSystemConnectionOption : ConnectionEndpointBase<CreateConnectionEndpoint>
{
    /// <summary>Initializes a new instance of the <see cref="CreateFileSystemConnectionOption"/> class.</summary>
    public CreateFileSystemConnectionOption() : base("CreateFileSystemConnection")
    {
    }
}
