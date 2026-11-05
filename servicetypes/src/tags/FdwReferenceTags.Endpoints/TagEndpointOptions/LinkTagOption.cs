using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace FdwReferenceTags.Endpoints.TagEndpointOptions;

/// <summary>The LinkTag endpoint.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagEndpoints), "LinkTag")]
public class LinkTagOption : TagEndpointBase<LinkTagEndpoint>
{
}
