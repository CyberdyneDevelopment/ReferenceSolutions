using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>Base for an endpoint over the proxy surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class ProxyEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ProxyEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected ProxyEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "ProxyEndpoint")
    {
    }
}

/// <summary>Base for a proxy endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class ProxyEndpointBase<TEndpoint> : ProxyEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="ProxyEndpointBase{TEndpoint}"/> class.</summary>
    protected ProxyEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
