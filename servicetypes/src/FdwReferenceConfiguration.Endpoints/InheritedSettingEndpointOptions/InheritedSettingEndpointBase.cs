using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceSettings.Endpoints.InheritedSettingEndpointOptions;

/// <summary>
/// Base for an endpoint over inherited and locked settings.
/// </summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type. The generic form below derives from this, so members close the generic while the
/// collection still has a closed type to collect.
/// </remarks>
public abstract class InheritedSettingEndpointBase : EndpointTypeOptionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InheritedSettingEndpointBase"/> class.
    /// </summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected InheritedSettingEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "InheritedSettingEndpoint")
    {
    }
}

/// <summary>
/// Base for an inherited-settings endpoint that takes its identity from the endpoint class itself.
/// </summary>
/// <typeparam name="TEndpoint">The endpoint class this option declares.</typeparam>
public abstract class InheritedSettingEndpointBase<TEndpoint> : InheritedSettingEndpointBase
    where TEndpoint : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InheritedSettingEndpointBase{TEndpoint}"/> class.
    /// </summary>
    protected InheritedSettingEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
