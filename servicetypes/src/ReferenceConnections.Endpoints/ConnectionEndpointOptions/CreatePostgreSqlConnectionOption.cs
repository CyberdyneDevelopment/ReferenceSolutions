using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The CreatePostgreSqlConnection endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "CreatePostgreSqlConnection")]
public class CreatePostgreSqlConnectionOption : ConnectionEndpointBase<CreatePostgreSqlConnectionEndpoint>
{
}
