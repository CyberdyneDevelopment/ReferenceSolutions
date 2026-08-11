using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceNodes.Endpoints.NodesEndpointOptions;

/// <summary>Base for an endpoint over the nodes surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class NodesEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="NodesEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected NodesEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "NodesEndpoint")
    {
    }
}

/// <summary>Base for a nodes endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class NodesEndpointBase<TEndpoint> : NodesEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="NodesEndpointBase{TEndpoint}"/> class.</summary>
    protected NodesEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
