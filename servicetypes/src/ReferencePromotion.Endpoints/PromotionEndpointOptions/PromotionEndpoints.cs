using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>
/// The endpoints over the promotion resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PromotionEndpointBase), typeof(IEndpointTypeOption), typeof(PromotionEndpoints))]
public partial class PromotionEndpoints : EndpointTypeCollectionBase<PromotionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
