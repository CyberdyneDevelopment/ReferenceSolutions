using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceDdl.Endpoints.DdlEndpointOptions;

/// <summary>The endpoints over the ddl surface.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "DdlEndpoints")]
[TypeCollection(typeof(DdlEndpointBase), typeof(IEndpointTypeOption), typeof(DdlEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "DdlEndpoints")]
public partial class DdlEndpoints : EndpointTypeCollectionBase<DdlEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
