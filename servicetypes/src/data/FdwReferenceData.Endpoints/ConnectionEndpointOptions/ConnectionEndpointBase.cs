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
    /// Initializes a new instance of the <see cref="ConnectionEndpointBase{TEndpoint}"/> class for the sole option wrapping its endpoint
    /// class, named as every endpoint option is: after <typeparamref name="TEndpoint"/> without its "Endpoint" suffix.
    /// </summary>
    protected ConnectionEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionEndpointBase{TEndpoint}"/> class with its own name.
    /// </summary>
    /// <param name="name">
    /// The option's own distinguishing name, for an option that shares <typeparamref name="TEndpoint"/> with others, e.g. several typed
    /// presets (MsSql, Http, FileSystem, ...) that all route through one shared endpoint class. The base id is derived from this name, not
    /// from <typeparamref name="TEndpoint"/>, so options sharing an endpoint still get distinct, stable ids.
    /// </param>
    protected ConnectionEndpointBase(string name)
        : base(name ?? throw new ArgumentNullException(nameof(name)), typeof(TEndpoint), $"The {name} endpoint.")
    {
    }
}
