using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceSchema.Endpoints.SchemaConnectionEndpointOptions;

/// <summary>The endpoints over the schema-connection resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SchemaConnectionEndpointBase), typeof(IEndpointTypeOption), typeof(SchemaConnectionEndpoints))]
public partial class SchemaConnectionEndpoints : EndpointTypeCollectionBase<SchemaConnectionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
