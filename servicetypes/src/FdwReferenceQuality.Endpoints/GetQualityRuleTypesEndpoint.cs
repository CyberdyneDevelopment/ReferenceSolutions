using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality.Endpoints;

namespace FdwReferenceQuality.Endpoints;

/// <summary>Concrete endpoint listing the registered quality rule types and severities.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404. Takes no provider: it answers from the TypeCollections themselves.
/// </remarks>
[ExcludeFromCodeCoverage]
public class GetQualityRuleTypesEndpoint : GetQualityRuleTypesEndpointBase
{
}
