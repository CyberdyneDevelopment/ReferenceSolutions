using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>
/// The endpoints over the data-set resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "DataSetEndpoints")]
[TypeCollection(typeof(DataSetEndpointBase), typeof(IEndpointTypeOption), typeof(DataSetEndpoints))]
public partial class DataSetEndpoints : EndpointTypeCollectionBase<DataSetEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
