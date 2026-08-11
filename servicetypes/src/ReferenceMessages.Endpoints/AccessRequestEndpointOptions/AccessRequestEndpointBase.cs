using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceMessages.Endpoints.AccessRequestEndpointOptions;

/// <summary>Base for an endpoint over the access-request resource.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type. The generic form below derives from this, so members close the generic while the
/// collection still has a closed type to collect.
/// </remarks>
public abstract class AccessRequestEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="AccessRequestEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected AccessRequestEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "AccessRequestEndpoint")
    {
    }
}

/// <summary>Base for an access-request endpoint that takes its identity from the endpoint class itself.</summary>
/// <typeparam name="TEndpoint">The endpoint class this option declares.</typeparam>
public abstract class AccessRequestEndpointBase<TEndpoint> : AccessRequestEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="AccessRequestEndpointBase{TEndpoint}"/> class.</summary>
    protected AccessRequestEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
