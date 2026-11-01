using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceSchema.Endpoints.SchemaEndpointOptions;

/// <summary>The endpoints over the schema resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "SchemaEndpoints")]
[TypeCollection(typeof(SchemaEndpointBase), typeof(IEndpointTypeOption), typeof(SchemaEndpoints))]
public partial class SchemaEndpoints : EndpointTypeCollectionBase<SchemaEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
