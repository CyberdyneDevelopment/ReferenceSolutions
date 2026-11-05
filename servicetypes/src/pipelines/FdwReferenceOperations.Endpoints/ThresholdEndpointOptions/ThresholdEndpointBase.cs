using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceThresholds.Endpoints.ThresholdEndpointOptions;

/// <summary>Base for an endpoint over the thresholds.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class ThresholdEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected ThresholdEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "ThresholdEndpoint")
    {
    }
}

/// <summary>Base for a threshold endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class ThresholdEndpointBase<TEndpoint> : ThresholdEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdEndpointBase{TEndpoint}"/> class.</summary>
    protected ThresholdEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
