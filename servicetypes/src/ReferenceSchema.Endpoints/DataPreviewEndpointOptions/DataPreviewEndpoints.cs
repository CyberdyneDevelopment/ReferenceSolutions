using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceSchema.Endpoints.DataPreviewEndpointOptions;

/// <summary>The endpoints over the data-preview resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "DataPreviewEndpoints")]
[TypeCollection(typeof(DataPreviewEndpointBase), typeof(IEndpointTypeOption), typeof(DataPreviewEndpoints))]
public partial class DataPreviewEndpoints : EndpointTypeCollectionBase<DataPreviewEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
