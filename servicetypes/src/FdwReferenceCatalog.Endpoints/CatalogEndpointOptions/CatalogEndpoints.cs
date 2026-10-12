using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceCatalog.Endpoints.CatalogEndpointOptions;

/// <summary>The CatalogEndpoints endpoint group.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "CatalogEndpoints")]
[TypeCollection(typeof(CatalogEndpointBase), typeof(IEndpointTypeOption), typeof(CatalogEndpoints))]
public partial class CatalogEndpoints : EndpointTypeCollectionBase<CatalogEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
