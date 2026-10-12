using Fdw.Collections.Attributes;

namespace SampleGroup.Endpoints.SampleGroupEndpointOptions;

/// <summary>
/// Declares the ListSampleGroup endpoint.
/// </summary>
/// <remarks>
/// This file is what puts the route on the wire. Auto-discovery is off, so an endpoint without a
/// declaring option compiles, passes any test that calls it directly, and returns 404 — there is
/// no error to read and nothing to grep for. Add the handler and this together, never one alone.
/// </remarks>
[TypeOption(typeof(SampleGroupEndpoints), "ListSampleGroup")]
public sealed class ListSampleGroupOption : SampleGroupEndpointBase<ListSampleGroupEndpoint>
{
}
