using Fdw.Collections.Attributes;
using FdwReferenceEndpoints.ApiReferenceEndpointOptions;

namespace SampleGroup.Endpoints.SampleGroupEndpointOptions;

/// <summary>
/// Declares the SampleGroup reference page's document: <c>GET /openapi/sample-permission</c>.
/// </summary>
/// <remarks>Auto-discovery is off, so this declaration is what routes the endpoint.</remarks>
[TypeOption(typeof(ApiReferenceEndpoints), "SampleGroupReferenceDocument")]
public sealed class SampleGroupReferenceDocumentOption : ApiReferenceEndpointBase<SampleGroupReferenceDocumentEndpoint>
{
}
