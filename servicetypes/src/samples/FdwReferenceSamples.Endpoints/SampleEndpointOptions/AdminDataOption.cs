using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSamples.Endpoints.SampleEndpointOptions;

/// <summary>The AdminData endpoint.</summary>
[WebMcpTool("admin_data", "Admin data.")]
[TypeOption(typeof(SampleEndpoints), "AdminData")]
public class AdminDataOption : SampleEndpointBase<AdminDataEndpoint>
{
}
