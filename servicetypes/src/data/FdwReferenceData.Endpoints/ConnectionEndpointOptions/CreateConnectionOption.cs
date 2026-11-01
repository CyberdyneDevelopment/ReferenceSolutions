using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Endpoints;
using Fdw.WebMcp.Abstractions;
using FdwReferenceConnections.MsSql;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The CreateConnection endpoint.</summary>
[WebMcpTool(
    "create_connection",
    "Create connection.",
    PresetFieldName = nameof(CreateConnectionRequest.ServiceType),
    PresetOptionType = typeof(MsSqlConnectionType))]
[TypeOption(typeof(ConnectionEndpoints), "CreateConnection")]
public class CreateConnectionOption : ConnectionEndpointBase<CreateConnectionEndpoint>
{
}
