using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSamples.Endpoints.SampleEndpointOptions;

/// <summary>The PublicData endpoint.</summary>
[WebMcpTool("public_data", "Public data.")]
[TypeOption(typeof(SampleEndpoints), "PublicData")]
public class PublicDataOption : SampleEndpointBase<PublicDataEndpoint>
{
}
