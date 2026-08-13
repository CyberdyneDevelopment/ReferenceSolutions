using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceEtlExecutions.Endpoints.EtlExecutionsEndpointOptions;

/// <summary>The ETL server's executions endpoints.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(EtlExecutionsEndpointBase), typeof(IEndpointTypeOption), typeof(EtlExecutionsEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "EtlExecutionsEndpoints")]
public partial class EtlExecutionsEndpoints : EndpointTypeCollectionBase<EtlExecutionsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
