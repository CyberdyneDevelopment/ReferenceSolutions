using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceDataSets.Endpoints.DataSetSourceEndpointOptions;

/// <summary>
/// The endpoints over the data-set-source resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "DataSetSourceEndpoints")]
[TypeCollection(typeof(DataSetSourceEndpointBase), typeof(IEndpointTypeOption), typeof(DataSetSourceEndpoints))]
public partial class DataSetSourceEndpoints : EndpointTypeCollectionBase<DataSetSourceEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
