using Fdw.Collections.Attributes;

namespace ReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>The UpdateProject endpoint.</summary>
[TypeOption(typeof(ProjectsEndpoints), "UpdateProject")]
public class UpdateProjectOption : ProjectsEndpointBase<UpdateProjectEndpoint>
{
}
