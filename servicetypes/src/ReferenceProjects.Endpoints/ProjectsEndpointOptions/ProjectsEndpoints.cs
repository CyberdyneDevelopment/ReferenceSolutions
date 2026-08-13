using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>The endpoints over the projects surface.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "ProjectsEndpoints")]
[TypeCollection(typeof(ProjectsEndpointBase), typeof(IEndpointTypeOption), typeof(ProjectsEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "ProjectsEndpoints")]
public partial class ProjectsEndpoints : EndpointTypeCollectionBase<ProjectsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
