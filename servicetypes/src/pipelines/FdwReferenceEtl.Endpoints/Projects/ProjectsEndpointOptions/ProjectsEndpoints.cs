using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>The endpoints over the projects surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "ProjectsEndpoints")]
[TypeCollection(typeof(ProjectsEndpointBase), typeof(IEndpointTypeOption), typeof(ProjectsEndpoints))]
public partial class ProjectsEndpoints : EndpointTypeCollectionBase<ProjectsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
