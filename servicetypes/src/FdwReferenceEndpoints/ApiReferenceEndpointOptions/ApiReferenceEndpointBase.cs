using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceEndpoints.ApiReferenceEndpointOptions;

/// <summary>Base for an API reference endpoint every host serves.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type. The generic form below derives from this, so members close the generic while the
/// collection still has a closed type to collect.
/// </remarks>
public abstract class ApiReferenceEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ApiReferenceEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected ApiReferenceEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "ApiReferenceEndpoint")
    {
    }
}

/// <summary>Base for an API reference endpoint that takes its identity from the endpoint class itself.</summary>
/// <typeparam name="TEndpoint">The endpoint class this option declares.</typeparam>
public abstract class ApiReferenceEndpointBase<TEndpoint> : ApiReferenceEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="ApiReferenceEndpointBase{TEndpoint}"/> class.</summary>
    protected ApiReferenceEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
