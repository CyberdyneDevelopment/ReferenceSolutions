using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace FdwReferenceTags.Endpoints.TagEndpointOptions;

/// <summary>The CreateTag endpoint.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TagEndpoints), "CreateTag")]
public class CreateTagOption : TagEndpointBase<CreateTagEndpoint>
{
}
