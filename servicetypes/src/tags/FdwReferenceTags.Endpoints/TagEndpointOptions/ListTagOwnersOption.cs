using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace FdwReferenceTags.Endpoints.TagEndpointOptions;

/// <summary>The ListTagOwners endpoint.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagEndpoints), "ListTagOwners")]
public class ListTagOwnersOption : TagEndpointBase<ListTagOwnersEndpoint>
{
}
