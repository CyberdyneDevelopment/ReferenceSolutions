using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceProjects.Endpoints.ProjectsEndpointOptions;

/// <summary>Base for an endpoint over the projects surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class ProjectsEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ProjectsEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected ProjectsEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "ProjectsEndpoint")
    {
    }
}

/// <summary>Base for a projects endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class ProjectsEndpointBase<TEndpoint> : ProjectsEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="ProjectsEndpointBase{TEndpoint}"/> class.</summary>
    protected ProjectsEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
