using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceConfiguration.Endpoints.ThemeEndpointOptions;

/// <summary>Base for the ThemeEndpoints options.</summary>
public abstract class ThemeEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ThemeEndpointBase"/> class.</summary>
    protected ThemeEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "ThemeEndpoint")
    {
    }
}

/// <summary>Base for the ThemeEndpoints options, naming its endpoint by type.</summary>
public abstract class ThemeEndpointBase<TEndpoint> : ThemeEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="ThemeEndpointBase{TEndpoint}"/> class.</summary>
    protected ThemeEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
