using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Endpoints;
using Fdw.WebMcp.Abstractions;
using FdwReferenceConnections.Http;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The CreateHttpConnection endpoint.</summary>
[WebMcpTool(
    "create_http_connection",
    "Create HTTP connection.",
    PresetFieldName = nameof(CreateConnectionRequest.ServiceType),
    PresetOptionType = typeof(HttpConnectionType))]
[TypeOption(typeof(ConnectionEndpoints), "CreateHttpConnection")]
public class CreateHttpConnectionOption : ConnectionEndpointBase<CreateConnectionEndpoint>
{
    /// <summary>Initializes a new instance of the <see cref="CreateHttpConnectionOption"/> class.</summary>
    public CreateHttpConnectionOption() : base("CreateHttpConnection")
    {
    }
}
