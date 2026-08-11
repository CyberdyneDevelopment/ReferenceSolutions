using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceAgentActions.Endpoints.AgentActionsEndpointOptions;

/// <summary>Base for an endpoint over the agentactions surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class AgentActionsEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="AgentActionsEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected AgentActionsEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "AgentActionsEndpoint")
    {
    }
}

/// <summary>Base for a agentactions endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class AgentActionsEndpointBase<TEndpoint> : AgentActionsEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="AgentActionsEndpointBase{TEndpoint}"/> class.</summary>
    protected AgentActionsEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
