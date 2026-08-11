using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>Base for an endpoint over the shared surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class SharedEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="SharedEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected SharedEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "SharedEndpoint")
    {
    }
}

/// <summary>Base for a shared endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class SharedEndpointBase<TEndpoint> : SharedEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="SharedEndpointBase{TEndpoint}"/> class.</summary>
    protected SharedEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
