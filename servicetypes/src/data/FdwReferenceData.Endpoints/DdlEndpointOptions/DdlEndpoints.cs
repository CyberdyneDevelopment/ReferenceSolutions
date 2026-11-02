using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceDdl.Endpoints.DdlEndpointOptions;

/// <summary>The endpoints over the ddl surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "DdlEndpoints")]
[TypeCollection(typeof(DdlEndpointBase), typeof(IEndpointTypeOption), typeof(DdlEndpoints))]
public partial class DdlEndpoints : EndpointTypeCollectionBase<DdlEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
