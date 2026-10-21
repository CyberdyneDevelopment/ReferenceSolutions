using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferencePipelines.Endpoints.ScriptEndpointOptions;

/// <summary>The stored script endpoints of the ETL host: only a host that references this package serves them (FDW-1442).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "ScriptEndpoints")]
[TypeCollection(typeof(ScriptEndpointBase), typeof(IEndpointTypeOption), typeof(ScriptEndpoints))]
public partial class ScriptEndpoints : EndpointTypeCollectionBase<ScriptEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
