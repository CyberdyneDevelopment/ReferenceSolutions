using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceLineage.Endpoints.LineageEndpointOptions;

/// <summary>Base for an endpoint over the lineage surface.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class LineageEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="LineageEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected LineageEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "LineageEndpoint")
    {
    }
}

/// <summary>Base for a lineage endpoint that takes its identity from the endpoint class.</summary>
/// <typeparam name="TEndpoint">The endpoint class.</typeparam>
public abstract class LineageEndpointBase<TEndpoint> : LineageEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="LineageEndpointBase{TEndpoint}"/> class.</summary>
    protected LineageEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
