using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceTenants.Endpoints.TenantsEndpointOptions;

/// <summary>Base for an endpoint over the tenants surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class TenantsEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="TenantsEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected TenantsEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "TenantsEndpoint")
    {
    }
}

/// <summary>Base for a tenants endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class TenantsEndpointBase<TEndpoint> : TenantsEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="TenantsEndpointBase{TEndpoint}"/> class.</summary>
    protected TenantsEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
