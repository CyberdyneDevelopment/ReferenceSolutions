using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace FdwReferenceTags.Endpoints.TagEndpointOptions;

/// <summary>The GetTag endpoint.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagEndpoints), "GetTag")]
public class GetTagOption : TagEndpointBase<GetTagEndpoint>
{
}
