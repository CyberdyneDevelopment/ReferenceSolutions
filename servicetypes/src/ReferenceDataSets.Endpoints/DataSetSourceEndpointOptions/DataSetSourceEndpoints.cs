using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceDataSets.Endpoints.DataSetSourceEndpointOptions;

/// <summary>
/// The endpoints over the data-set-source resource.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "DataSetSourceEndpoints")]
[TypeCollection(typeof(DataSetSourceEndpointBase), typeof(IEndpointTypeOption), typeof(DataSetSourceEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "DataSetSourceEndpoints")]
public partial class DataSetSourceEndpoints : EndpointTypeCollectionBase<DataSetSourceEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
