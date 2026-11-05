using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace FdwReferenceTags.Endpoints.TagEndpointOptions;

/// <summary>The GetThingTags endpoint.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagEndpoints), "GetThingTags")]
public class GetThingTagsOption : TagEndpointBase<GetThingTagsEndpoint>
{
}
