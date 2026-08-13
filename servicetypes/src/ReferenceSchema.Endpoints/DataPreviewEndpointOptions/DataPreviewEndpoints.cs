using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceSchema.Endpoints.DataPreviewEndpointOptions;

/// <summary>The endpoints over the data-preview resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataPreviewEndpointBase), typeof(IEndpointTypeOption), typeof(DataPreviewEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "DataPreviewEndpoints")]
public partial class DataPreviewEndpoints : EndpointTypeCollectionBase<DataPreviewEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
