using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>Base for an endpoint over the nfl surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class NflEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="NflEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected NflEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "NflEndpoint")
    {
    }
}

/// <summary>Base for a nfl endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class NflEndpointBase<TEndpoint> : NflEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="NflEndpointBase{TEndpoint}"/> class.</summary>
    protected NflEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
