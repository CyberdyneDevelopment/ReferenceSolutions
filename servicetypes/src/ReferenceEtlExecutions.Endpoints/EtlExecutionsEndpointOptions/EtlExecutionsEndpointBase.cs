using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceEtlExecutions.Endpoints.EtlExecutionsEndpointOptions;

/// <summary>Base for an endpoint over the ETL executions surface.</summary>
public abstract class EtlExecutionsEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="EtlExecutionsEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected EtlExecutionsEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "EtlExecutionsEndpoint")
    {
    }
}

/// <summary>Base for one that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class EtlExecutionsEndpointBase<TEndpoint> : EtlExecutionsEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="EtlExecutionsEndpointBase{TEndpoint}"/> class.</summary>
    protected EtlExecutionsEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
