using Fdw.Collections.Attributes;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The DeletePlayer endpoint.</summary>
[TypeOption(typeof(NflEndpoints), "DeletePlayer")]
public class DeletePlayerOption : NflEndpointBase<DeletePlayerEndpoint>
{
}
