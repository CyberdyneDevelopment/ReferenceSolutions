using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Endpoints;
using Fdw.WebMcp.Abstractions;
using FdwReferenceConnections.PostgreSql;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The CreatePostgreSqlConnection endpoint.</summary>
[WebMcpTool(
    "create_postgre_sql_connection",
    "Create postgre SQL connection.",
    PresetFieldName = nameof(CreateConnectionRequest.ServiceType),
    PresetOptionType = typeof(PostgreSqlConnectionType))]
[TypeOption(typeof(ConnectionEndpoints), "CreatePostgreSqlConnection")]
public class CreatePostgreSqlConnectionOption : ConnectionEndpointBase<CreateConnectionEndpoint>
{
    /// <summary>Initializes a new instance of the <see cref="CreatePostgreSqlConnectionOption"/> class.</summary>
    public CreatePostgreSqlConnectionOption() : base("CreatePostgreSqlConnection")
    {
    }
}
