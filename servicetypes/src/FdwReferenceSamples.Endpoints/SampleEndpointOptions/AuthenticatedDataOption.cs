using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSamples.Endpoints.SampleEndpointOptions;

/// <summary>The AuthenticatedData endpoint.</summary>
[WebMcpTool("authenticated_data", "Authenticated data.")]
[TypeOption(typeof(SampleEndpoints), "AuthenticatedData")]
public class AuthenticatedDataOption : SampleEndpointBase<AuthenticatedDataEndpoint>
{
}
