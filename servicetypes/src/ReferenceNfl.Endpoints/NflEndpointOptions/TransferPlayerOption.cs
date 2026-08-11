using Fdw.Collections.Attributes;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The TransferPlayer endpoint.</summary>
[TypeOption(typeof(NflEndpoints), "TransferPlayer")]
public class TransferPlayerOption : NflEndpointBase<TransferPlayerEndpoint>
{
}
