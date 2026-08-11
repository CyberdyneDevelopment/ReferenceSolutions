using Fdw.Collections.Attributes;

namespace ReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>The CreateProject endpoint.</summary>
[TypeOption(typeof(ProjectsEndpoints), "CreateProject")]
public class CreateProjectOption : ProjectsEndpointBase<CreateProjectEndpoint>
{
}
