using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceDataSets.Endpoints.CategoryEndpointOptions;

/// <summary>
/// The endpoints over the category resource: the category tree, its types, and creating, renaming, moving and retiring a category.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "CategoryEndpoints")]
[TypeCollection(typeof(CategoryEndpointBase), typeof(IEndpointTypeOption), typeof(CategoryEndpoints))]
public partial class CategoryEndpoints : EndpointTypeCollectionBase<CategoryEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
