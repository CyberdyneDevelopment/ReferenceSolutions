using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceEtlLineage.Endpoints.EtlLineageEndpointOptions;

/// <summary>Base for an endpoint over the ETL lineage surface.</summary>
public abstract class EtlLineageEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="EtlLineageEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected EtlLineageEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "EtlLineageEndpoint")
    {
    }
}

/// <summary>Base for one that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class EtlLineageEndpointBase<TEndpoint> : EtlLineageEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="EtlLineageEndpointBase{TEndpoint}"/> class.</summary>
    protected EtlLineageEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
