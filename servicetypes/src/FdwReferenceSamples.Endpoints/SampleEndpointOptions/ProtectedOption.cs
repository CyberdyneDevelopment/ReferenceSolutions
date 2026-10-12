using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSamples.Endpoints.SampleEndpointOptions;

/// <summary>The Protected endpoint.</summary>
[WebMcpTool("protected", "Protected.")]
[TypeOption(typeof(SampleEndpoints), "Protected")]
public class ProtectedOption : SampleEndpointBase<ProtectedEndpoint>
{
}
