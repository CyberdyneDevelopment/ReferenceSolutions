using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceHealth.Endpoints.HealthEndpointOptions;

/// <summary>Base for an endpoint over the health surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class HealthEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="HealthEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected HealthEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "HealthEndpoint")
    {
    }
}

/// <summary>Base for a health endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class HealthEndpointBase<TEndpoint> : HealthEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="HealthEndpointBase{TEndpoint}"/> class.</summary>
    protected HealthEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
