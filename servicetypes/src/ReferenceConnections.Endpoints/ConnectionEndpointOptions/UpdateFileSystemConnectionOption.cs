using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The UpdateFileSystemConnection endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "UpdateFileSystemConnection")]
public class UpdateFileSystemConnectionOption : ConnectionEndpointBase<UpdateFileSystemConnectionEndpoint>
{
}
