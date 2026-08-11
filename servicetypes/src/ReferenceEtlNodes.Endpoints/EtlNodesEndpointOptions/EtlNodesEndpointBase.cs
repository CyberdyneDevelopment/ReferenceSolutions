using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceEtlNodes.Endpoints.EtlNodesEndpointOptions;

/// <summary>Base for an endpoint over the ETL nodes surface.</summary>
public abstract class EtlNodesEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="EtlNodesEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected EtlNodesEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "EtlNodesEndpoint")
    {
    }
}

/// <summary>Base for one that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class EtlNodesEndpointBase<TEndpoint> : EtlNodesEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="EtlNodesEndpointBase{TEndpoint}"/> class.</summary>
    protected EtlNodesEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
