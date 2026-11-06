using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceTags.Endpoints.TagEndpointOptions;

/// <summary>Base for the options that declare this app's tag endpoints.</summary>
public abstract class TagEndpointBase : EndpointTypeOptionBase
{
    /// <inheritdoc />
    protected TagEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "TagEndpoint")
    {
    }
}

/// <summary>Base for the options that declare this app's tag endpoints.</summary>
/// <typeparam name="TEndpoint">The endpoint class this option declares.</typeparam>
public abstract class TagEndpointBase<TEndpoint> : TagEndpointBase
    where TEndpoint : class
{
    /// <inheritdoc />
    protected TagEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
