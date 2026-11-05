using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace FdwReferenceTags.Endpoints.TagEndpointOptions;

/// <summary>The UnlinkTag endpoint.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagEndpoints), "UnlinkTag")]
public class UnlinkTagOption : TagEndpointBase<UnlinkTagEndpoint>
{
}
