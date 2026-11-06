using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace FdwReferenceTags.Endpoints.TagEndpointOptions;

/// <summary>The DeleteTag endpoint.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagEndpoints), "DeleteTag")]
public class DeleteTagOption : TagEndpointBase<DeleteTagEndpoint>
{
}
