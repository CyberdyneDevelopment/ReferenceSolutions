using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The endpoints over the data-store-container resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataStoreContainerEndpointBase), typeof(IEndpointTypeOption), typeof(DataStoreContainerEndpoints))]
public partial class DataStoreContainerEndpoints : EndpointTypeCollectionBase<DataStoreContainerEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
