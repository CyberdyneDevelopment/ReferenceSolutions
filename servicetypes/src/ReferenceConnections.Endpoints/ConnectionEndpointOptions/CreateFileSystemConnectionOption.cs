using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The CreateFileSystemConnection endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "CreateFileSystemConnection")]
public class CreateFileSystemConnectionOption : ConnectionEndpointBase<CreateFileSystemConnectionEndpoint>
{
}
