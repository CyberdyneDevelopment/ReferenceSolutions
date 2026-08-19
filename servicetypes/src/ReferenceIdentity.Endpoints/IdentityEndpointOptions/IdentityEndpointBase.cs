using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceIdentity.Endpoints.IdentityEndpointOptions;

/// <summary>Base for an option declaring a managed-identity endpoint.</summary>
public abstract class IdentityEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityEndpointBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="endpointType">The endpoint this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected IdentityEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "IdentityEndpoint")
    {
    }
}

/// <summary>Base for an option declaring <typeparamref name="TEndpoint"/>.</summary>
/// <typeparam name="TEndpoint">The endpoint being declared.</typeparam>
public abstract class IdentityEndpointBase<TEndpoint> : IdentityEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="IdentityEndpointBase{TEndpoint}"/> class.</summary>
    protected IdentityEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
