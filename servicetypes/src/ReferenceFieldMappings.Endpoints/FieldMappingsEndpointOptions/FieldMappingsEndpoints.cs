using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceFieldMappings.Endpoints.FieldMappingsEndpointOptions;

/// <summary>The endpoints over the fieldmappings surface.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "FieldMappingsEndpoints")]
[TypeCollection(typeof(FieldMappingsEndpointBase), typeof(IEndpointTypeOption), typeof(FieldMappingsEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "FieldMappingsEndpoints")]
public partial class FieldMappingsEndpoints : EndpointTypeCollectionBase<FieldMappingsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
