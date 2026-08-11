using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceSchema.Endpoints.DataPreviewEndpointOptions;

/// <summary>Base for an endpoint over the data-preview resource.</summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type. The generic form below derives from this, so members close the generic while the
/// collection still has a closed type to collect.
/// </remarks>
public abstract class DataPreviewEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="DataPreviewEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected DataPreviewEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "DataPreviewEndpoint")
    {
    }
}

/// <summary>Base for a data-preview endpoint that takes its identity from the endpoint class itself.</summary>
/// <typeparam name="TEndpoint">The endpoint class this option declares.</typeparam>
public abstract class DataPreviewEndpointBase<TEndpoint> : DataPreviewEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="DataPreviewEndpointBase{TEndpoint}"/> class.</summary>
    protected DataPreviewEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
