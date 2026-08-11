using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>Base for an endpoint over the escalation surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class EscalationEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="EscalationEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected EscalationEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "EscalationEndpoint")
    {
    }
}

/// <summary>Base for a escalation endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class EscalationEndpointBase<TEndpoint> : EscalationEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="EscalationEndpointBase{TEndpoint}"/> class.</summary>
    protected EscalationEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
