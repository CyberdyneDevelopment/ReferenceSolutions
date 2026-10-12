using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace SampleGroup.Endpoints.SampleGroupEndpointOptions;

/// <summary>Base for an option declaring a SampleGroup endpoint.</summary>
public abstract class SampleGroupEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="SampleGroupEndpointBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="endpointType">The endpoint this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected SampleGroupEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "SampleGroupEndpoint")
    {
    }
}

/// <summary>Base for an option declaring <typeparamref name="TEndpoint"/>.</summary>
/// <typeparam name="TEndpoint">The endpoint being declared.</typeparam>
public abstract class SampleGroupEndpointBase<TEndpoint> : SampleGroupEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="SampleGroupEndpointBase{TEndpoint}"/> class.</summary>
    protected SampleGroupEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
