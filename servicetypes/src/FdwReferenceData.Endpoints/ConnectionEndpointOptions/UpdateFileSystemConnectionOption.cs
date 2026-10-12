using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The UpdateFileSystemConnection endpoint.</summary>
[WebMcpTool("update_file_system_connection", "Update file system connection.")]
[TypeOption(typeof(ConnectionEndpoints), "UpdateFileSystemConnection")]
public class UpdateFileSystemConnectionOption : ConnectionEndpointBase<UpdateConnectionEndpoint>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateFileSystemConnectionOption"/> class.</summary>
    public UpdateFileSystemConnectionOption() : base("UpdateFileSystemConnection")
    {
    }
}
