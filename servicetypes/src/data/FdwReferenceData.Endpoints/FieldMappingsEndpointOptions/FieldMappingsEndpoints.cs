using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceFieldMappings.Endpoints.FieldMappingsEndpointOptions;

/// <summary>The endpoints over the fieldmappings surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "FieldMappingsEndpoints")]
[TypeCollection(typeof(FieldMappingsEndpointBase), typeof(IEndpointTypeOption), typeof(FieldMappingsEndpoints))]
public partial class FieldMappingsEndpoints : EndpointTypeCollectionBase<FieldMappingsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
