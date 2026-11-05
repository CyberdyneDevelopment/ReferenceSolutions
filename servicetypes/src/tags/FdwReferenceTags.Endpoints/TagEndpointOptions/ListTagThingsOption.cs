using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace FdwReferenceTags.Endpoints.TagEndpointOptions;

/// <summary>The ListTagThings endpoint.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagEndpoints), "ListTagThings")]
public class ListTagThingsOption : TagEndpointBase<ListTagThingsEndpoint>
{
}
