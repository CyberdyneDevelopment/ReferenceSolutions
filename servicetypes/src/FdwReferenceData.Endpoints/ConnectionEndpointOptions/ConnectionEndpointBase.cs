using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>
/// Base for an endpoint over the connection resource.
/// </summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type. The generic form below derives from this, so members close the generic while the
/// collection still has a closed type to collect.
/// </remarks>
public abstract class ConnectionEndpointBase : EndpointTypeOptionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionEndpointBase"/> class.
    /// </summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected ConnectionEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "ConnectionEndpoint")
    {
    }
}

/// <summary>
/// Base for a connection endpoint that takes its identity from the endpoint class itself.
/// </summary>
/// <typeparam name="TEndpoint">The endpoint class this option declares.</typeparam>
public abstract class ConnectionEndpointBase<TEndpoint> : ConnectionEndpointBase
    where TEndpoint : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionEndpointBase{TEndpoint}"/> class.
    /// </summary>
    /// <param name="name">
    /// The option's own distinguishing name. Required when more than one option closes over the
    /// same <typeparamref name="TEndpoint"/> — e.g. several typed presets (MsSql, Http, FileSystem,
    /// ...) that all route through one shared endpoint class. The base id is derived from this name,
    /// not from <typeparamref name="TEndpoint"/>, so options sharing an endpoint still get distinct,
    /// stable ids. Omit it only when this option is the sole one wrapping its endpoint class.
    /// </param>
    protected ConnectionEndpointBase(string? name = null)
        : base(name ?? DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {name ?? DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
