using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceVisualization.Endpoints.VisualizationEndpointOptions;

/// <summary>Base for an endpoint over the visualization surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class VisualizationEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="VisualizationEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected VisualizationEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "VisualizationEndpoint")
    {
    }
}

/// <summary>Base for a visualization endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class VisualizationEndpointBase<TEndpoint> : VisualizationEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="VisualizationEndpointBase{TEndpoint}"/> class.</summary>
    protected VisualizationEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
