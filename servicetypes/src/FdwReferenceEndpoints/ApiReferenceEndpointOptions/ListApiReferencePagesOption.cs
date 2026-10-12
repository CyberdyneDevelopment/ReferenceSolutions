using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using FdwReferenceEndpoints.ApiReference;

namespace FdwReferenceEndpoints.ApiReferenceEndpointOptions;

/// <summary>The list of API reference pages the caller may open — GET /reference/pages.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ApiReferenceEndpoints), "ListApiReferencePages")]
public class ListApiReferencePagesOption : ApiReferenceEndpointBase<ListApiReferencePagesEndpoint>
{
}
