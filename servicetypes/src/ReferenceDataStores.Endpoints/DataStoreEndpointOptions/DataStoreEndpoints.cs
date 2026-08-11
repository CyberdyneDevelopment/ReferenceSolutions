using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceDataStores.Endpoints.DataStoreEndpointOptions;

/// <summary>The endpoints over the data-store resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataStoreEndpointBase), typeof(IEndpointTypeOption), typeof(DataStoreEndpoints))]
public partial class DataStoreEndpoints : EndpointTypeCollectionBase<DataStoreEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
