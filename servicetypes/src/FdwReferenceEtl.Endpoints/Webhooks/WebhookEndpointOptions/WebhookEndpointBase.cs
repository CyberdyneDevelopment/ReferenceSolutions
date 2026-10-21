using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferencePipelines.Endpoints.WebhookEndpointOptions;

/// <summary>The base of the webhook trigger endpoints of the ETL host (FDW-1884).</summary>
public abstract class WebhookEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="WebhookEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint it declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected WebhookEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "WebhookEndpoint")
    {
    }
}

/// <summary>A webhook trigger endpoint option for one endpoint type.</summary>
/// <typeparam name="TEndpoint">The endpoint it declares.</typeparam>
public abstract class WebhookEndpointBase<TEndpoint> : WebhookEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="WebhookEndpointBase{TEndpoint}"/> class.</summary>
    protected WebhookEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
