using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The endpoints over the data-store-container resource.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "DataStoreContainerEndpoints")]
[TypeCollection(typeof(DataStoreContainerEndpointBase), typeof(IEndpointTypeOption), typeof(DataStoreContainerEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "DataStoreContainerEndpoints")]
public partial class DataStoreContainerEndpoints : EndpointTypeCollectionBase<DataStoreContainerEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
