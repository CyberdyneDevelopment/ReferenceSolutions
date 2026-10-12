using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The UpdatePostgreSqlConnection endpoint.</summary>
[WebMcpTool("update_postgre_sql_connection", "Update postgre SQL connection.")]
[TypeOption(typeof(ConnectionEndpoints), "UpdatePostgreSqlConnection")]
public class UpdatePostgreSqlConnectionOption : ConnectionEndpointBase<UpdateConnectionEndpoint>
{
    /// <summary>Initializes a new instance of the <see cref="UpdatePostgreSqlConnectionOption"/> class.</summary>
    public UpdatePostgreSqlConnectionOption() : base("UpdatePostgreSqlConnection")
    {
    }
}
