using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferencePipelines.Endpoints.ScriptEndpointOptions;

/// <summary>The base of the stored script endpoints of the ETL host (FDW-1442).</summary>
public abstract class ScriptEndpointBase : EndpointTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ScriptEndpointBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="endpointType">The endpoint it declares.</param>
    /// <param name="description">What the endpoint does.</param>
    protected ScriptEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "ScriptEndpoint")
    {
    }
}

/// <summary>A stored script endpoint option for one endpoint type.</summary>
/// <typeparam name="TEndpoint">The endpoint it declares.</typeparam>
public abstract class ScriptEndpointBase<TEndpoint> : ScriptEndpointBase
    where TEndpoint : class
{
    /// <summary>Initializes a new instance of the <see cref="ScriptEndpointBase{TEndpoint}"/> class.</summary>
    protected ScriptEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
