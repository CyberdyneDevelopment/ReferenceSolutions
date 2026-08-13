using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>
/// The endpoints over the data-set resource.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "DataSetEndpoints")]
[TypeCollection(typeof(DataSetEndpointBase), typeof(IEndpointTypeOption), typeof(DataSetEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "DataSetEndpoints")]
public partial class DataSetEndpoints : EndpointTypeCollectionBase<DataSetEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
