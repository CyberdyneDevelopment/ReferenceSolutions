using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceSchema.Endpoints.SchemaEndpointOptions;

/// <summary>The endpoints over the schema resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SchemaEndpointBase), typeof(IEndpointTypeOption), typeof(SchemaEndpoints))]
public partial class SchemaEndpoints : EndpointTypeCollectionBase<SchemaEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
