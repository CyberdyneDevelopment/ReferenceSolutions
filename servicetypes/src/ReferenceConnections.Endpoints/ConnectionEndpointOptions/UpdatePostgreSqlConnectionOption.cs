using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The UpdatePostgreSqlConnection endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "UpdatePostgreSqlConnection")]
public class UpdatePostgreSqlConnectionOption : ConnectionEndpointBase<UpdatePostgreSqlConnectionEndpoint>
{
}
