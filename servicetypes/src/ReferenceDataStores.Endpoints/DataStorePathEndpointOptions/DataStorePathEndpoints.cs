using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceDataStores.Endpoints.DataStorePathEndpointOptions;

/// <summary>The endpoints over the data-store-path resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "DataStorePathEndpoints")]
[TypeCollection(typeof(DataStorePathEndpointBase), typeof(IEndpointTypeOption), typeof(DataStorePathEndpoints))]
public partial class DataStorePathEndpoints : EndpointTypeCollectionBase<DataStorePathEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
