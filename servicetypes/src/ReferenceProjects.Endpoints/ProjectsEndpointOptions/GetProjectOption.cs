using Fdw.Collections.Attributes;

namespace ReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>The GetProject endpoint.</summary>
[TypeOption(typeof(ProjectsEndpoints), "GetProject")]
public class GetProjectOption : ProjectsEndpointBase<GetProjectEndpoint>
{
}
