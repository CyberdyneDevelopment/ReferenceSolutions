using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceEtlJobs.Endpoints.EtlJobsEndpointOptions;

/// <summary>Base for an endpoint over the ETL jobs surface.</summary>
public abstract class EtlJobsEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="EtlJobsEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected EtlJobsEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "EtlJobsEndpoint")
    {
    }
}

/// <summary>Base for one that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class EtlJobsEndpointBase<TEndpoint> : EtlJobsEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="EtlJobsEndpointBase{TEndpoint}"/> class.</summary>
    protected EtlJobsEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
