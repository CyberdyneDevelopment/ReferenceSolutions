using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>
/// The endpoints over the promotion resource.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "PromotionEndpoints")]
[TypeCollection(typeof(PromotionEndpointBase), typeof(IEndpointTypeOption), typeof(PromotionEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "PromotionEndpoints")]
public partial class PromotionEndpoints : EndpointTypeCollectionBase<PromotionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
