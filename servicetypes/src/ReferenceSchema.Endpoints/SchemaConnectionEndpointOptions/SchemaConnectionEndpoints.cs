using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceSchema.Endpoints.SchemaConnectionEndpointOptions;

/// <summary>The endpoints over the schema-connection resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "SchemaConnectionEndpoints")]
[TypeCollection(typeof(SchemaConnectionEndpointBase), typeof(IEndpointTypeOption), typeof(SchemaConnectionEndpoints))]
public partial class SchemaConnectionEndpoints : EndpointTypeCollectionBase<SchemaConnectionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
